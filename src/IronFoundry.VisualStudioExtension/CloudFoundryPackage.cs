﻿namespace IronFoundry.VisualStudioExtension
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Interop;
    using EnvDTE;
    using GalaSoft.MvvmLight.Messaging;
    using IronFoundry.Vcap;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Models;
    using Ui.Controls.Model;
    using Ui.Controls.Mvvm;
    using Ui.Controls.Utilities;
    using Ui.Controls.ViewModel;
    using Ui.Controls.ViewModel.Push;
    using Ui.Controls.Views;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidCloudFoundryPkgString)]
    [ProvideBindingPath]
    public sealed class CloudFoundryPackage : Package
    {
        private IVsMonitorSelection vsMonitorSelection;
        private DTE dte;
        private ICloudFoundryProvider provider;

        public CloudFoundryPackage()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));
            dte = GetService(typeof(SDTE)) as DTE;

            if (provider == null)
            {
                var preferences = new PreferencesProvider("VisualStudio2010");
                provider = new CloudFoundryProvider(preferences);
            }

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                mcs.AddCommand(new MenuCommand(CloudFoundryExplorer,
                               new CommandID(GuidList.guidCloudFoundryCmdSet,
                               (int)PkgCmdIDList.cmdidCloudFoundryExplorer)));

                mcs.AddCommand(new MenuCommand(PushApplication,
                               new CommandID(GuidList.guidCloudFoundryCmdSet,
                               (int)PkgCmdIDList.cmdidPushCloudFoundryApplication)));

                mcs.AddCommand(new MenuCommand(UpdateApplication,
                               new CommandID(GuidList.guidCloudFoundryCmdSet,
                               (int)PkgCmdIDList.cmdidUpdateCloudFoundryApplication)));
            }
        }

        private void CloudFoundryExplorer(object sender, EventArgs e)
        {
            var window = new Explorer();
            var helper = new WindowInteropHelper(window);
            helper.Owner = (IntPtr)(dte.MainWindow.HWnd);
            window.ShowDialog();
        }

        private void PushApplication(object sender, EventArgs e)
        {
            Project project = vsMonitorSelection.GetActiveProject();
            if (project != null)
            {
                var cloudGuid = GetCurrentCloudGuid(project);
                var projectDirectories = GetProjectDirectories(project);

                Messenger.Default.Register<NotificationMessageAction<Guid>>(this,
                    message =>
                    {
                        if (message.Notification.Equals(Messages.SetPushAppData))
                            message.Execute(cloudGuid);
                    });    
            
                Messenger.Default.Register<NotificationMessageAction<string>>(this,
                    message =>
                    {
                        if (message.Notification.Equals(Messages.SetPushAppDirectory))
                            message.Execute(projectDirectories.DeployFromPath);
                    });

                var window = new Push();
                var helper = new WindowInteropHelper(window);
                helper.Owner = (IntPtr)(dte.MainWindow.HWnd);
                var result = window.ShowDialog();

                if (result.GetValueOrDefault())
                {
                    PushViewModel modelData = null;
                    Messenger.Default.Send(new NotificationMessageAction<PushViewModel>(Messages.GetPushAppData, model => modelData = model));
                    SetCurrentCloudGuid(project, modelData.SelectedCloud.ID);

                    PerformAction("Push Application", project, modelData.SelectedCloud, projectDirectories, (c, d) =>
                                c.Push(modelData.Name, modelData.Url, Convert.ToUInt16(modelData.Instances), d,
                                    Convert.ToUInt32(modelData.SelectedMemory),
                                    modelData.ApplicationServices.Select(provisionedService => provisionedService.Name).ToArray()));
                }
            }
        }

        private void UpdateApplication(object sender, EventArgs e)
        {
            var project = vsMonitorSelection.GetActiveProject();
            if (project == null) 
                return;
            var cloudGuid = GetCurrentCloudGuid(project);
            var projectDirectories = GetProjectDirectories(project);

            Messenger.Default.Register<NotificationMessageAction<Guid>>(this,
                                                                        message =>
                                                                        {
                                                                            if (message.Notification.Equals(Messages.SetUpdateAppData))
                                                                                message.Execute(cloudGuid);
                                                                        });

            Messenger.Default.Register<NotificationMessageAction<string>>(this,
                                                                          message =>
                                                                          {
                                                                              if (message.Notification.Equals(Messages.SetPushAppDirectory))
                                                                                  message.Execute(projectDirectories.DeployFromPath);
                                                                          });

            var window = new Update();
            var helper = new WindowInteropHelper(window);
            helper.Owner = (IntPtr)(dte.MainWindow.HWnd);
            var result = window.ShowDialog();

            if (!result.GetValueOrDefault()) 
                return;

            UpdateViewModel modelData = null;
            Messenger.Default.Send(new NotificationMessageAction<UpdateViewModel>(Messages.GetUpdateAppData, model => modelData = model));

            SetCurrentCloudGuid(project, modelData.SelectedCloud.ID);
            PerformAction("Update Application", project, modelData.SelectedCloud, projectDirectories, (c, d) =>
            {
                c.Update(modelData.SelectedApplication.Name, d);
                c.Restart(new Application {Name = modelData.SelectedApplication.Name});
            });
        }

        private void PerformAction(string actionName, Project project, Cloud cloud, ProjectDirectories dir,
            Action<IVcapClient, DirectoryInfo> action)
        {
            var worker = new BackgroundWorker();

            Messenger.Default.Register<NotificationMessageAction<string>>(this,
                message =>
                {
                    if (message.Notification.Equals(Messages.SetProgressData))
                        message.Execute(actionName);
                });

            var window = new ProgressDialog();
            var dispatcher = window.Dispatcher;
            var helper = new WindowInteropHelper(window);
            helper.Owner = (IntPtr)(dte.MainWindow.HWnd);
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (s, args) =>
            {
                if (worker.CancellationPending) { args.Cancel = true; return; }

                Messenger.Default.Send(new ProgressMessage(0, "Starting " + actionName));

                if (worker.CancellationPending) { args.Cancel = true; return; }

                if (!Directory.Exists(dir.StagingPath))
                {
                    dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressMessage(10, "Creating Staging Path")))); 
                    Directory.CreateDirectory(dir.StagingPath);
                }

                if (worker.CancellationPending) { args.Cancel = true; return; }

                if (Directory.Exists(dir.DeployFromPath))
                {
                    dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressMessage(10, "Creating Precompiled Site Path"))));
                    Directory.Delete(dir.DeployFromPath, true);
                }

                if (worker.CancellationPending) { args.Cancel = true; return; }
                
                dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressMessage(30, "Preparing Compiler"))));

                VsWebSite.VSWebSite site = project.Object as VsWebSite.VSWebSite;
                if (site != null)
                {
                    site.PreCompileWeb(dir.DeployFromPath, true);
                }
                else
                {
                    string frameworkPath = (site == null) ? project.GetFrameworkPath() : String.Empty;

                    if (worker.CancellationPending) { args.Cancel = true; return; }
                    string objDir = Path.Combine(dir.ProjectDirectory, "obj");
                    if (Directory.Exists(objDir))
                    {
                        Directory.Delete(objDir, true); // NB: this can cause precompile errors
                    }

                    var process = new System.Diagnostics.Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = Path.Combine(frameworkPath, "aspnet_compiler.exe"),
                            Arguments = String.Format("-nologo -v / -p \"{0}\" -f -u -c \"{1}\"", dir.ProjectDirectory, dir.DeployFromPath),
                            CreateNoWindow = true,
                            ErrorDialog = false,
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        }
                    };

                    if (worker.CancellationPending) { args.Cancel = true; return; }
                    dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressMessage(40, "Precompiling Site"))));
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string message = "Asp Compile Error";
                        if (false == String.IsNullOrEmpty(output))
                        {
                            message += ": " + output;
                        }
                        dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressError(message))));
                        return;
                    }
                }

                dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressMessage(50, "Logging in to Cloud Foundry"))));

                if (worker.CancellationPending) { args.Cancel = true; return; }

                var client = new VcapClient(cloud);
                try
                {
                    client.Login();
                }
                catch (Exception e)
                {
                    dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressError("Failure: " + e.Message))));
                    return;
                }

                dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressMessage(75, "Sending to " + cloud.Url))));
                if (worker.CancellationPending) { args.Cancel = true; return; }

                try
                {
                    action(client, new DirectoryInfo(dir.DeployFromPath));
                }
                catch (Exception e)
                {
                    dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressError("Failure: " + e.Message))));
                    return;
                }
                dispatcher.BeginInvoke((Action)(() => Messenger.Default.Send(new ProgressMessage(100, actionName + " complete."))));
            };

            worker.RunWorkerAsync();
            if (!window.ShowDialog().GetValueOrDefault())
            {
                worker.CancelAsync();
            }
        }

        private static ProjectDirectories GetProjectDirectories(Project project)
        {
            var projectProperties = project.DTE.Properties["Environment", "ProjectsAndSolution"];
            var defaultProjectLocation = projectProperties.Item("ProjectsLocation").Value as string;
            var stagingPath = Path.Combine(defaultProjectLocation, "CloudFoundry_Staging");
            var projectDirectory = Path.GetDirectoryName(project.FullName);
            var projectName = Path.GetFileNameWithoutExtension(projectDirectory).ToLower();
            var precompiledSitePath = Path.Combine(stagingPath, projectName);
            return new ProjectDirectories()
            {
                StagingPath = stagingPath,
                ProjectDirectory = projectDirectory,
                DeployFromPath = precompiledSitePath
            };
        }

        private static Guid GetCurrentCloudGuid(Project project)
        {
            var cloudGuid = Guid.Empty;
            var cloudId = project.GetGlobalVariable("CloudId");
            if (!Guid.TryParse(cloudId, out cloudGuid))
                cloudGuid = Guid.Empty;
            return cloudGuid;
        }

        private static void SetCurrentCloudGuid(Project project, Guid guid)
        {
            project.SetGlobalVariable("CloudId", guid.ToString());
        }
    }

    public class ProjectDirectories
    {
        public string StagingPath { get; set; }
        public string DeployFromPath { get; set; }
        public string ProjectDirectory { get; set; }
    }
}

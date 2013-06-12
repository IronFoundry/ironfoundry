﻿namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using IronFoundry.Warden.Containers;

    public class PowershellCommand : ProcessCommand
    {
        private const string powershellArgFmt = "-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -WindowStyle Hidden -File \"{0}\"";

        public PowershellCommand(Container container, string[] arguments, bool shouldImpersonate)
            : base(container, arguments, shouldImpersonate)
        {
            if (base.arguments.IsNullOrEmpty())
            {
                throw new ArgumentException("powershell: command must have at least one argument.");
            }
        }

        protected override TaskCommandResult DoExecute()
        {
            using (var ps1File = container.TempFileInContainer(".ps1"))
            {
                File.WriteAllLines(ps1File.FullName, container.ConvertToPathsWithin(arguments), Encoding.ASCII);
                string psArgs = String.Format(powershellArgFmt, ps1File.FullName);
                return base.RunProcess(ps1File.DirectoryName, "powershell.exe", psArgs);
            }
        }
    }
}

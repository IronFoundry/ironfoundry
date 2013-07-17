﻿namespace IronFoundry.Warden.Containers
{
    using System;
    using System.IO;
    using System.Security.AccessControl;
    using IronFoundry.Warden.Configuration;

    public class ContainerDirectory
    {
        private readonly DirectoryInfo containerDirectory;

        public string Path { get { return containerDirectory.FullName; }}

        public ContainerDirectory(ContainerHandle handle, ContainerUser user, bool shouldCreate = false)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            if (shouldCreate)
            {
                this.containerDirectory = CreateContainerDirectory(handle, user);
            }
            else
            {
                this.containerDirectory = FindContainerDirectory(handle);
            }
        }

        public void Delete()
        {
            containerDirectory.Delete(true);
        }

        public static void CleanUp(string handle)
        {
            try
            {
                var items = GetContainerDirectoryInfo(new ContainerHandle(handle));
                Directory.Delete(items.Item2, true);
            }
            catch { }
        }

        public static implicit operator string(ContainerDirectory containerDirectory)
        {
            return containerDirectory.ToString();
        }

        public override string ToString()
        {
            return containerDirectory.FullName;
        }

        private static DirectoryInfo CreateContainerDirectory(ContainerHandle handle, ContainerUser user)
        {
            var dirInfo = GetContainerDirectoryInfo(handle);

            var inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var accessRule = new FileSystemAccessRule(user, FileSystemRights.FullControl, inheritanceFlags,
                PropagationFlags.InheritOnly, AccessControlType.Allow);

            DirectoryInfo containerBaseInfo = dirInfo.Item1;
            DirectorySecurity security = containerBaseInfo.GetAccessControl();
            security.AddAccessRule(accessRule);

            string containerDirectory = dirInfo.Item2;
            return Directory.CreateDirectory(containerDirectory, security);
        }

        private static DirectoryInfo FindContainerDirectory(ContainerHandle handle)
        {
            var dirInfo = GetContainerDirectoryInfo(handle);
            if (Directory.Exists(dirInfo.Item2))
            {
                return new DirectoryInfo(dirInfo.Item2);
            }
            else
            {
                throw new WardenException("Directory '{0}' does not exist!", dirInfo.Item2);
            }
        }

        private static Tuple<DirectoryInfo, string> GetContainerDirectoryInfo(ContainerHandle handle)
        {
            var config = new WardenConfig();

            string containerBasePath = config.ContainerBasePath;
            string containerDirectory = System.IO.Path.Combine(containerBasePath, handle);

            return new Tuple<DirectoryInfo, string>(new DirectoryInfo(containerBasePath), containerDirectory);
        }
    }
}

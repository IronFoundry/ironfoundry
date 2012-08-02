﻿namespace IronFoundry.Dea.Services
{
    using System;
    using System.IdentityModel.Selectors;
    using System.ServiceModel;
    using IronFoundry.Dea.Properties;
    using IronFoundry.Misc.Configuration;

    public class CustomUserNamePasswordValidator : UserNamePasswordValidator
    {
        private readonly ServiceCredential credentials;

        public CustomUserNamePasswordValidator(ServiceCredential credentials)
        {
            this.credentials = credentials;
        }

        public override void Validate(string userName, string password)
        {
            if (userName.IsNullOrWhiteSpace() || password.IsNullOrWhiteSpace())
            {
                throw new ArgumentException();
            }
            if (userName != credentials.Username)
            {
                throw new FaultException(String.Format(Resources.FilesServiceValidator_InvalidUser_Fmt, userName));
            }
            if (password != credentials.Password)
            {
                throw new FaultException(String.Format(Resources.FilesServiceValidator_InvalidPassword_Fmt, password));
            }
        }
    }
}
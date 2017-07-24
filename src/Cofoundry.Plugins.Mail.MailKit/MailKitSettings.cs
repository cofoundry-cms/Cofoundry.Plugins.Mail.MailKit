using Cofoundry.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cofoundry.Plugins.Mail.MailKit
{
    public class MailKitSettings : PluginConfigurationSettingsBase
    {
        public MailKitSettings()
        {
            Host = "localhost";
            Port = 25;
            CertificateValidationMode = CertificateValidationMode.Default;
        }

        /// <summary>
        /// Indicates whether the plugin should be disabled, which means services
        /// will not be bootstrapped. Defaults to false.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// The user name to authenticate with the smtp server. If left empty
        /// then no auth will be used.
        /// </summary>
        public string UserName { get; set; }


        /// <summary>
        /// The password use when authenticating with the smtp server. 
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Host address of the smtp server to connect with. Defaults to localhost.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The port to connect to the smtp server on. Defaults to 25.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Indicates whether ssl should be used to connect to the host.
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// Used to configure how the ssl certificate is validated.
        /// </summary>
        public CertificateValidationMode CertificateValidationMode { get; set; }
    }
}

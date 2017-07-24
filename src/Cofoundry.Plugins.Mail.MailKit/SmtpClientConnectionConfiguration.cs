using Cofoundry.Core.Configuration;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Cofoundry.Plugins.Mail.MailKit
{
    /// <summary>
    /// Used to configure the MailKit SmtpClient and customize the connection process.
    /// </summary>
    public class SmtpClientConnectionConfiguration : ISmtpClientConnectionConfiguration
    {
        private readonly MailKitSettings _mailKitSettings;

        public SmtpClientConnectionConfiguration(
            MailKitSettings mailKitSettings
            )
        {
            _mailKitSettings = mailKitSettings;
        }

        /// <summary>
        /// Initializes the SmtpClient after it has been created.
        /// </summary>
        /// <param name="smtpClient">Instance to initialize.</param>
        public virtual void Initialize(SmtpClient smtpClient)
        {
            const string OAUTH2_MECHANISM = "XOAUTH2";

            if (smtpClient == null) throw new ArgumentNullException(nameof(smtpClient));

            // Note: all samples disable the OAuth2/XOAUTH2 authentication mechanism. Will leave it out until we have a use case
            if (smtpClient.AuthenticationMechanisms.Contains(OAUTH2_MECHANISM))
            {
                smtpClient.AuthenticationMechanisms.Remove(OAUTH2_MECHANISM);
            }

            switch (_mailKitSettings.CertificateValidationMode)
            {
                case CertificateValidationMode.All:
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    break;
                case CertificateValidationMode.ValidOnly:
                    smtpClient.ServerCertificateValidationCallback = ValidateValidCertificatesOnly;
                    break;
                case CertificateValidationMode.Default:
                    break;
                default:
                    throw new InvalidConfigurationException(nameof(_mailKitSettings.CertificateValidationMode), "Unknown CertificateValidationMode.");
            }
        }

        /// <summary>
        /// Opens the SmtpClient connection to the configured host.
        /// </summary>
        /// <param name="smtpClient">Instance to connect with.</param>
        public virtual void Connect(SmtpClient smtpClient)
        {
            if (smtpClient == null) throw new ArgumentNullException(nameof(smtpClient));

            if (smtpClient.IsConnected) return;

            smtpClient.Connect(_mailKitSettings.Host, _mailKitSettings.Port, _mailKitSettings.EnableSsl);

            if (!string.IsNullOrWhiteSpace(_mailKitSettings.UserName) && !smtpClient.IsAuthenticated)
            {
                smtpClient.Authenticate(_mailKitSettings.UserName, _mailKitSettings.Password);
            }
        }

        /// <summary>
        /// Opens the SmtpClient connection to the configured host.
        /// </summary>
        /// <param name="smtpClient">Instance to connect with.</param>
        public virtual async Task ConnectAsync(SmtpClient smtpClient)
        {
            if (smtpClient == null) throw new ArgumentNullException(nameof(smtpClient));

            if (smtpClient.IsConnected) return;

            await smtpClient.ConnectAsync(_mailKitSettings.Host, _mailKitSettings.Port, _mailKitSettings.EnableSsl);

            if (!string.IsNullOrWhiteSpace(_mailKitSettings.UserName))
            {
                await smtpClient.AuthenticateAsync(_mailKitSettings.UserName, _mailKitSettings.Password);
            }
        }

        /// <summary>
        /// Closes the SmtpClient connection to the configured host.
        /// </summary>
        /// <param name="smtpClient">Instance to close the connection for.</param>
        public virtual void Disconnect(SmtpClient smtpClient)
        {
            if (smtpClient == null) throw new ArgumentNullException(nameof(smtpClient));

            smtpClient.Disconnect(true);
        }

        /// <summary>
        /// Closes the SmtpClient connection to the configured host.
        /// </summary>
        /// <param name="smtpClient">Instance to close the connection for.</param>
        public virtual Task DisconnectAsync(SmtpClient smtpClient)
        {
            if (smtpClient == null) throw new ArgumentNullException(nameof(smtpClient));

            return smtpClient.DisconnectAsync(true);
        }

        private bool ValidateValidCertificatesOnly(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }

    }
}

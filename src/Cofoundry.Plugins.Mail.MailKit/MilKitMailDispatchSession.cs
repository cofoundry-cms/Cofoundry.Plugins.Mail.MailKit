using Cofoundry.Core;
using Cofoundry.Core.Mail;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cofoundry.Plugins.Mail.MailKit
{
    /// <summary>
    /// Mail dispatch session that uses System.Net.Mail to
    /// dispatch email.
    /// </summary>
    public class MilKitMailDispatchSession : IMailDispatchSession
    {
        private readonly Queue<MimeMessage> _mailQueue = new Queue<MimeMessage>();
        private readonly Lazy<SmtpClient> _mailClient;
        private readonly MailSettings _mailSettings;
        private readonly IPathResolver _pathResolver;
        private readonly ISmtpClientConnectionConfiguration _smtpClientConnectionConfiguration;

        private bool isDisposing = false;

        public MilKitMailDispatchSession(
            MailSettings mailSettings,
            IPathResolver pathResolver,
            ISmtpClientConnectionConfiguration smtpClientConnectionConfiguration
            )
        {
            _mailSettings = mailSettings;
            _pathResolver = pathResolver;
            _mailClient = new Lazy<SmtpClient>(CreateSmtpMailClient);
            _smtpClientConnectionConfiguration = smtpClientConnectionConfiguration;
        }

        public void Add(MailMessage mailMessage)
        {
            var messageToSend = FormatMessage(mailMessage);
            _mailQueue.Enqueue(messageToSend);
        }

        public void Flush()
        {
            ValidateNotDisposed();

            if (_mailSettings.SendMode == MailSendMode.LocalDrop)
            {
                FlushToLocalDrop();
                return;
            }

            try
            {
                _smtpClientConnectionConfiguration.Connect(_mailClient.Value);

                while (_mailQueue.Count > 0)
                {
                    var mailItem = _mailQueue.Dequeue();
                    if (mailItem != null && _mailSettings.SendMode != MailSendMode.DoNotSend)
                    {
                        _mailClient.Value.Send(mailItem);
                    }
                }
            }
            finally
            {
                _smtpClientConnectionConfiguration.Disconnect(_mailClient.Value);
            }
        }

        public async Task FlushAsync()
        {
            ValidateNotDisposed();

            if (_mailSettings.SendMode == MailSendMode.LocalDrop)
            {
                FlushToLocalDrop();
                return;
            }

            try
            {
                await _smtpClientConnectionConfiguration.ConnectAsync(_mailClient.Value);

                while (_mailQueue.Count > 0)
                {
                    var mailItem = _mailQueue.Dequeue();
                    if (mailItem != null && _mailSettings.SendMode != MailSendMode.DoNotSend)
                    {
                        await _mailClient.Value.SendAsync(mailItem);
                    }
                }
            }
            finally
            {
                await _smtpClientConnectionConfiguration.DisconnectAsync(_mailClient.Value);
            }
        }

        public void Dispose()
        {
            isDisposing = true;
            if (_mailClient.IsValueCreated)
            {
                _mailClient.Value?.Dispose();
            }
        }

        #region private methods

        private void ValidateNotDisposed()
        {
            if (isDisposing)
            {
                throw new InvalidOperationException("Cannot perform the operation because the object has been disposed");
            }
        }

        /// <summary>
        /// see https://stackoverflow.com/a/39933156/716689
        /// </summary>
        private void FlushToLocalDrop()
        {
            var pickupDirectory = GetMailDropPath();

            while (_mailQueue.Count > 0)
            {
                var mailItem = _mailQueue.Dequeue();
                if (mailItem != null)
                {
                    var path = Path.Combine(pickupDirectory, Guid.NewGuid().ToString() + ".eml");

                    using (var stream = new FileStream(path, FileMode.CreateNew))
                    {
                        mailItem.WriteTo(stream);
                        return;
                    }
                }
            }
        }


        private SmtpClient CreateSmtpMailClient()
        {
            if (isDisposing) return null;
            return new SmtpClient();
        }
        
        private MimeMessage FormatMessage(MailMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var messageToSend = new MimeMessage();

            var toAddress = GetMailToAddress(message);
            messageToSend.To.Add(toAddress);
            messageToSend.Subject = message.Subject;
            if (message.From != null)
            {
                messageToSend.From.Add(CreateMailAddress(message.From.Address, message.From.DisplayName));
            }
            else
            {
                messageToSend.From.Add(CreateMailAddress(_mailSettings.DefaultFromAddress, _mailSettings.DefaultFromAddressDisplayName));
            }
            SetMessageBody(messageToSend, message.HtmlBody, message.TextBody);

            return messageToSend;
        }

        private MailboxAddress GetMailToAddress(MailMessage message)
        {
            MailboxAddress toAddress;
            if (_mailSettings.SendMode == MailSendMode.SendToDebugAddress)
            {
                if (string.IsNullOrEmpty(_mailSettings.DebugEmailAddress))
                {
                    throw new Exception("MailSendMode.SendToDebugAddress requested but Cofoundry:SmtpMail:DebugEmailAddress setting is not defined.");
                }
                toAddress = CreateMailAddress(_mailSettings.DebugEmailAddress, message.To.DisplayName);
            }
            else
            {
                toAddress = new MailboxAddress(message.To.DisplayName, message.To.Address);
            }
            return toAddress;
        }

        private void SetMessageBody(MimeMessage message, string bodyHtml, string bodyText)
        {
            var hasHtmlBody = !string.IsNullOrWhiteSpace(bodyHtml);
            var hasTextBody = !string.IsNullOrWhiteSpace(bodyText);
            if (!hasHtmlBody && !hasTextBody)
            {
                throw new ArgumentException("An email must have either a html or text body");
            }

            if (hasHtmlBody && !hasTextBody)
            {
                message.Body = new TextPart(TextFormat.Html) { Text = bodyHtml };
            }
            else if (hasTextBody && !hasHtmlBody)
            {
                message.Body = new TextPart(TextFormat.Plain) { Text = bodyText };
            }
            else
            {
                var alternative = new Multipart("alternative");
                alternative.Add(new TextPart(TextFormat.Plain) { Text = bodyText });
                alternative.Add(new TextPart(TextFormat.Html) { Text = bodyHtml });

                message.Body = alternative;
            }
        }

        private MailboxAddress CreateMailAddress(string email, string displayName)
        {
            MailboxAddress mailAddress = null;
            try
            {
                if (string.IsNullOrEmpty(displayName))
                {
                    mailAddress = new MailboxAddress(email);
                }
                else
                {
                    mailAddress = new MailboxAddress(displayName, email);
                }
            }
            catch (ParseException ex)
            {
                throw new InvalidMailAddressException(email, displayName, ex);
            }

            return mailAddress;
        }

        private string GetMailDropPath()
        {
            if (string.IsNullOrEmpty(_mailSettings.MailDropDirectory))
            {
                throw new Exception("Cofoundry:Mail:MailDropDirectory configuration has been requested and is not set.");
            }

            var mailDropDirectory = _pathResolver.MapPath(_mailSettings.MailDropDirectory);
            if (!Directory.Exists(mailDropDirectory))
            {
                Directory.CreateDirectory(mailDropDirectory);
            }

            return mailDropDirectory;
        }

        #endregion
    }
}

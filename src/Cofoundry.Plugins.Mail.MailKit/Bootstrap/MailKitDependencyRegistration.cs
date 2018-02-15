using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cofoundry.Core.DependencyInjection;
using Cofoundry.Core.Mail;

namespace Cofoundry.Plugins.Mail.MailKit
{
    public class MailKitDependencyRegistration : IDependencyRegistration
    {
        public void Register(IContainerRegister container)
        {
            if (container.Configuration.GetValue<bool>("Cofoundry:Plugins:MailKit:Disabled")) return;

            var overrideOptions = RegistrationOptions.Override();

            container
                .Register<IMailDispatchService, MailKitMailDispatchService>(overrideOptions)
                .Register<ISmtpClientConnectionConfiguration, SmtpClientConnectionConfiguration>()
                ; 
        }
    }
}

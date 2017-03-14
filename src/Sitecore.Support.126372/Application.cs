using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Web
{
    public class Application : Sitecore.Web.Application
    {
        public virtual void Application_Start(object sender, EventArgs args)
        {
            Sitecore.DependencyInjection.ServiceLocator.MakeReadonly();
        }
    }
}
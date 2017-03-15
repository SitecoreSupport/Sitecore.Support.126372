using Sitecore;
using Sitecore.Web;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Pipelines.EndSession;
using Sitecore.Pipelines.SessionEnd;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace Sitecore.Support.Web
{
    public class Application : HttpApplication
    {
        public virtual void Application_Start(object sender, EventArgs args)
        {
            ServiceLocator.MakeReadonly();
        }

        public virtual void FormsAuthentication_OnAuthenticate(object sender, FormsAuthenticationEventArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            string frameworkVersion = this.GetFrameworkVersion();
            if (!string.IsNullOrEmpty(frameworkVersion) && frameworkVersion.StartsWith("v4.", StringComparison.InvariantCultureIgnoreCase))
            {
                args.User = Context.User;
            }
        }

        private string GetFrameworkVersion()
        {
            try
            {
                return RuntimeEnvironment.GetSystemVersion();
            }
            catch (Exception exception)
            {
                Log.Error("Cannot get framework version", exception, this);
                return string.Empty;
            }
        }

        public static void RaiseSessionEndEvent(HttpApplication context)
        {
            Assert.ArgumentNotNull(context, "context");
            object hostContext = CallContext.HostContext;
            try
            {
                CallContext.HostContext = new HttpContext(new SessionEndWorkerRequest("SESSION END", string.Empty, new StringWriter()));
                HttpContext.Current.ApplicationInstance = context;
                HttpSessionState session = HttpContext.Current.Session;
                HttpContext.Current.Items["AspSession"] = context.Session;
                try
                {
                    Event.RaiseEvent("sessionEnd:starting", new object[] { new { Current = HttpContext.Current } });
                }
                catch (Exception exception)
                {
                    Log.Error("Cannot execute 'sessionEnd:starting' event", exception, typeof(Application));
                }
                try
                {
                    SessionEndPipeline.Run(new SessionEndArgs(HttpContext.Current));
                }
                catch (Exception exception2)
                {
                    Log.Error("SessionEndPipeline failed.", exception2, typeof(Application));
                }
                try
                {
                    Event.RaiseEvent("sessionEnd:postSessionEnd:starting", new object[] { new { Current = HttpContext.Current } });
                }
                catch (Exception exception3)
                {
                    Log.Error("Cannot execute 'sessionEnd:postSessionEnd:starting' event", exception3, typeof(Application));
                }
                try
                {
                    PostSessionEndPipeline.Run(new PostSessionEndArgs(HttpContext.Current));
                }
                catch (Exception exception4)
                {
                    Log.Error("PostSessionEndPipeline failed.", exception4, typeof(Application));
                }
            }
            finally
            {
                try
                {
                    Event.RaiseEvent("sessionEnd:ended", new object[] { new { Current = HttpContext.Current } });
                }
                catch (Exception exception5)
                {
                    Log.Error("Cannot execute 'sessionEnd:ended' event", exception5, typeof(Application));
                }
                CallContext.HostContext = hostContext;
            }
        }

        public void Session_End()
        {
            RaiseSessionEndEvent(this);
        }

    }
}
using System;
using library;
using log4net;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace app
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(root + "log4net.config"));
            log4net.Config.XmlConfigurator.Configure(
                log4net.LogManager.CreateRepository(
                    Assembly.GetEntryAssembly(),
                    typeof(log4net.Repository.Hierarchy.Hierarchy)),
                log4netConfig["log4net"]
            );

            var controller = new SalesforceController();
            var task = controller.RunSample();
            task.Wait();
        }

        public static string root {
        get {
            var appRoot = AppContext.BaseDirectory.Substring(0,AppContext.BaseDirectory.LastIndexOf("/bin"));
            return appRoot.Substring(0,appRoot.LastIndexOf("/")+1);
        }
    }
    }
}

using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;
using Serilog.Core;

namespace SmartFleet.TcpWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private static Logger _log;
         /// <summary>
        /// 
        /// </summary>
        // ReSharper disable once MethodNameNotMeaningful
        public override void Run()
        {
            InitLog();
            // init teltonika server 
             DependencyRegistrar.ResolveDependencies();
        }
       
        private static void InitLog()
        {
            _log = new LoggerConfiguration()
                // .WriteTo.Console()
                .WriteTo.File("tcp-worker-role.txt")
                .CreateLogger();
        }
         public override bool OnStart()
        {
            // Définir le nombre maximum de connexions simultanées
            ServicePointManager.DefaultConnectionLimit = 12;

            // Pour plus d'informations sur la gestion des modifications de configuration
            // consultez la rubrique MSDN à l'adresse https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("SmartFleet.TcpWorker has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("SmartFleet.TcpWorker is stopping");

            _cancellationTokenSource.Cancel();
            _runCompleteEvent.WaitOne();
           // _listener.Stop();
            //_bus.StopAsync();
            base.OnStop();

            Trace.TraceInformation("SmartFleet.TcpWorker has stopped");
        }

      
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using BPS.Service.Queue;

namespace BPS.Service
{
    public partial class Service1 : ServiceBase
    {
        private CancellationTokenSource _stopToken;
        private Task _workerTask;

        public Service1()
        {
            InitializeComponent();
        }

        internal void StartDebug()
        {
            OnStart(Array.Empty<string>());
        }

        internal void StopDebug()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            _stopToken = new CancellationTokenSource();
            var runtime = new WorkerRuntime();
            _workerTask = runtime.RunAsync(_stopToken.Token);
        }

        protected override void OnStop()
        {
            if (_stopToken != null)
            {
                _stopToken.Cancel();
            }

            if (_workerTask != null)
            {
                try
                {
                    _workerTask.Wait(TimeSpan.FromSeconds(15));
                }
                catch
                {
                    // Service stop must continue even if worker cancellation throws.
                }
            }

            if (_stopToken != null)
            {
                _stopToken.Dispose();
                _stopToken = null;
            }

            _workerTask = null;
        }
    }
}

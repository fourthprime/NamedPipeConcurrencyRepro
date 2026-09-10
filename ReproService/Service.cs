// SERVICE
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ReproLib;

namespace ReproService
{
	static class Program
	{
		static void Main(string[] args)
		{
			var singleton = new Service();
			var uriAddress = MakeUriAddress();

			Console.WriteLine("Startup info: listening at '{0}'", uriAddress);
			var host = new ServiceHost(singleton, uriAddress);

			Console.WriteLine("Startup info: adding endpoint named '{0}'", ReproConstants.ServiceName);
			host.AddServiceEndpoint(typeof(IReproService), MakeBinding(), ReproConstants.ServiceName);

			host.Open();

			Console.WriteLine("Startup complete - awaiting shutdown...");
			singleton.SpinUntilShutdownRequested();

			Console.WriteLine("Shutting down");
			try
			{
				host.Close();
			}
			catch
			{
				try
				{
					host.Abort();
				}
				catch
				{
				}
			}
			Console.WriteLine("Shutdown complete");
		}

		static NetNamedPipeBinding MakeBinding() => new NetNamedPipeBinding()
		{
			MaxReceivedMessageSize = 1 << 30,
			TransferMode = TransferMode.Buffered,
			ReaderQuotas = XmlDictionaryReaderQuotas.Max,
			OpenTimeout = TimeSpan.MaxValue,
			CloseTimeout = TimeSpan.MaxValue,
			SendTimeout = TimeSpan.MaxValue,
			ReceiveTimeout = TimeSpan.MaxValue,
		};

		static Uri MakeUriAddress() => new Uri(ReproConstants.ServiceBasePath);
	}

	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	class Service : IReproService
	{
		readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(initialState: false);

		public void SpinUntilShutdownRequested()
		{
			_shutdownEvent.WaitOne();
		}

		ReproResult ReproOperation(ReproRequest request, int sleepMs)
		{
			Console.WriteLine("Got request: '{0}'", request.Input);
			if (request.Input == "shutdown")
			{
				_shutdownEvent.Set();
			}

			if (sleepMs > 0)
			{
				Thread.Sleep(sleepMs);
			}

			return new ReproResult() { Result = $"acknowledged '{request.Input}'" };
		}

		public async Task<ReproResult> ReproOperationAsync(ReproRequest request)
		{
			await Task.Yield();
			await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);

			return ReproOperation(request, sleepMs: 0);
		}
	}
}

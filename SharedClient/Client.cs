// CLIENT
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ReproLib;

namespace ReproClient
{
	static class ReproConfig
	{
		// repros the issue (.NET 8): create a new client per call and do not throttle concurrency
		public const int TotalRequests = 100;
		public const int MaxParallelism = TotalRequests;
	}

	static class Program
	{
		static async Task Main(string[] args)
		{
			int numberOfRequests = ReproConfig.TotalRequests;

			Console.WriteLine("CLR dir - {0}", RuntimeEnvironment.GetRuntimeDirectory());
			Console.WriteLine("Number of processors: {0}", Environment.ProcessorCount);
			Console.WriteLine("Executing {0} requests with max concurrency {1}", numberOfRequests, concurrencyLimiter.CurrentCount);

			var tasks = Enumerable.Range(0, numberOfRequests).Select(i => SendAsync($"...{i}"));

			await Task.WhenAll(tasks);

			Console.WriteLine("client repro done");
		}

		static readonly SemaphoreSlim concurrencyLimiter = new SemaphoreSlim(initialCount: ReproConfig.MaxParallelism);

		static async Task<ReproResult> SendAsync(string input)
		{
			ReproRequest request = new ReproRequest { Input = input };
			Client clientForThisRequest = null;

			// without this the repro never occurs due to each connection opening synchronously
			await Task.Yield();

			await concurrencyLimiter.WaitAsync();
			try
			{
				clientForThisRequest = Client.Create();

				return await clientForThisRequest.Host.ReproOperationAsync(request);
			}
			catch (CommunicationException communicationException) when (
				communicationException.InnerException is PipeException pipeException &&
				pipeException.Message.EndsWith(" but the connect failed: The operation has timed out."))
			{
				// In debugger: set a breakpoint here, or enable 1st chance exceptions to break on the above
				Console.WriteLine("UNEXPECTED: timeout on busy named pipe");
				throw;
			}
			finally
			{
				clientForThisRequest?.ForceClose();
				concurrencyLimiter.Release();
			}
		}
	}

	class Client : ClientBase<IReproService>
	{
		Client(Binding binding, EndpointAddress remoteAddress)
			: base(binding, remoteAddress)
		{
		}

		public IReproService Host { get { return Channel; } }

		public void ForceClose()
		{
			try
			{
				Close();
			}
			catch
			{
				// WCF can throw when you close a connection - just abort it
				Abort();
			}
		}

		public static Client Create() => new Client(MakeBinding(), MakeAddress());

		static NetNamedPipeBinding MakeBinding() => new NetNamedPipeBinding()
		{
			MaxReceivedMessageSize = 1 << 30,
			TransferMode = default(TransferMode),
			ReaderQuotas = XmlDictionaryReaderQuotas.Max,
			OpenTimeout = TimeSpan.FromSeconds(10),
			CloseTimeout = TimeSpan.FromSeconds(1),
			SendTimeout = TimeSpan.MaxValue,
			ReceiveTimeout = TimeSpan.MaxValue,
		};

		static EndpointAddress MakeAddress() => new EndpointAddress(new Uri(ReproConstants.ServiceFullPath));
	}
}

using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ReproLib
{
	public static class ReproConstants
	{
		public const string ServiceName = "IReproService";
		public const string ServiceBasePath = "net.pipe://localhost/Repro";
		public const string ServiceFullPath = "net.pipe://localhost/Repro/" + ServiceName;
	}

	[DataContract]
	public class ReproRequest
	{
		[DataMember]
		public string Input { get; set; }
	}

	[DataContract]
	public class ReproResult
	{
		[DataMember]
		public string Result { get; set; }
	}

	[ServiceContract(Namespace = "http://contoso.net/repro")]
	public interface IReproService
	{
		[OperationContract]
		Task<ReproResult> ReproOperationAsync(ReproRequest request);
	}
}

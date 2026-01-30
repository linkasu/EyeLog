using System.Runtime.Serialization;

namespace EyeLog.Models
{
    [DataContract]
    internal class TimeoutRequest
    {
        [DataMember(Name = "clientId")]
        public string ClientId { get; set; }

        [DataMember(Name = "clickTimeoutMs")]
        public int ClickTimeoutMs { get; set; }
    }

    [DataContract]
    internal class TimeoutResponse
    {
        [DataMember(Name = "clientId")]
        public string ClientId { get; set; }

        [DataMember(Name = "clickTimeoutMs")]
        public int ClickTimeoutMs { get; set; }
    }
}

using System.Runtime.Serialization;

namespace EyeLog.Models
{
    [DataContract]
    internal class HealthResponse
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}

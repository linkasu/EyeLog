using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EyeLog.Models
{
    [DataContract]
    internal class BoundsRequest
    {
        [DataMember(Name = "clientId", EmitDefaultValue = false)]
        public string ClientId { get; set; }

        [DataMember(Name = "bounds")]
        public List<BoundDto> Bounds { get; set; }
    }

    [DataContract]
    internal class BoundsResponse
    {
        [DataMember(Name = "clientId")]
        public string ClientId { get; set; }

        [DataMember(Name = "indices")]
        public int[] Indices { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
}

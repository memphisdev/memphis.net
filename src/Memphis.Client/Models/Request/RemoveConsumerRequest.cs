using System.Runtime.Serialization;

namespace Memphis.Client.Models.Request
{
    [DataContract]
    internal sealed class RemoveConsumerRequest
    {
        [DataMember(Name = "name")]
        public string ConsumerName { get; set; }
        
        [DataMember(Name = "station_name")]
        public string StationName { get; set; }
    }
}
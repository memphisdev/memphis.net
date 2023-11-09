﻿#nullable disable

namespace Memphis.Client.Models.Request;

[DataContract]
internal sealed class PmAckMsg
{
    [DataMember(Name = "id")]
    public string Id { get; set; }
    
    [DataMember(Name = "cg_name")]
    public string ConsumerGroupName { get; set; }
}
using Memphis.Client.Validators;

namespace Memphis.Client.Constants;
internal class MemphisStations
{
    public const string MEMPHIS_PRODUCER_CREATIONS = "$memphis_producer_creations";
    public const string MEMPHIS_CONSUMER_CREATIONS = "$memphis_consumer_creations";
    public const string MEMPHIS_STATION_CREATIONS = "$memphis_station_creations";
    public const string MEMPHIS_PRODUCER_DESTRUCTIONS = "$memphis_producer_destructions";
    public const string MEMPHIS_CONSUMER_DESTRUCTIONS = "$memphis_consumer_destructions";
    public const string MEMPHIS_SCHEMA_ATTACHMENTS = "$memphis_schema_attachments";
    public const string MEMPHIS_SCHEMA_DETACHMENTS = "$memphis_schema_detachments";
    public const string MEMPHIS_NOTIFICATIONS = "$memphis_notifications";
    public const string MEMPHIS_STATION_DESTRUCTION = "$memphis_station_destructions";
}

internal class MemphisHeaders
{
    public const string MESSAGE_ID = "msg-id";
    public const string MEMPHIS_PRODUCED_BY = "$memphis_producedBy";
    public const string MEMPHIS_CONNECTION_ID = "$memphis_connectionId";
}

internal class MemphisSubscriptions
{
    public const string DLS_PREFIX = "$memphis_dls_";
}

internal class MemphisSubjects
{
    public const string PM_RESEND_ACK_SUBJ = "$memphis_pm_acks";
    public const string MEMPHIS_SCHEMA_UPDATE = "$memphis_schema_updates_";
    public const string SDK_CLIENTS_UPDATE = "$memphis_sdk_clients_updates";
    public const string MEMPHIS_SCHEMA_VERSE_DLS = "$memphis_schemaverse_dls";
    public const string SCHEMA_CREATION = "$memphis_schema_creations";
    public const string FUNCTIONS_UPDATE = "$memphis_functions_updates_";
    public const string NACKED_DLS = "$memphis_nacked_dls";
}

internal static class MemphisSchemaTypes
{
    public const string NONE = "";
    public const string JSON = "json";
    public const string GRAPH_QL = "graphql";
    public const string PROTO_BUF = "protobuf";
    internal const string AVRO = "avro";

    internal static ValidatorType ToValidator(this string schemaType)
    {
        return schemaType switch
        {
            JSON => ValidatorType.JSON,
            GRAPH_QL => ValidatorType.GRAPHQL,
            PROTO_BUF => ValidatorType.PROTOBUF,
            AVRO => ValidatorType.AVRO,
            _ => throw new MemphisException($"Schema type: {schemaType} is not supported")
        };
    }
}

internal static class MemphisSdkClientUpdateTypes
{
    public const string SEND_NOTIFICATION = "send_notification";
    public const string SCHEMA_VERSE_TO_DLS = "schemaverse_to_dls";
    public const string REMOVE_STATION = "remove_station";
}

internal static class MemphisGlobalVariables
{
    public const string GLOBAL_ACCOUNT_NAME = "$memphis";
    public const uint MURMUR_HASH_SEED = 31;
    public const int MAX_BATCH_SIZE = 5000;
    public const int JETSTREAM_OPERATION_TIMEOUT = 30;
}

internal static class MemphisRequestVersions
{
    public const int LastProducerCreationRequestVersion = 4;
    public const int LastProducerDestroyRequestVersion = 1;
    public const int LastConsumerCreationRequestVersion = 4;
    public const int LastConsumerDestroyRequestVersion = 1;
}
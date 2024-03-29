namespace Memphis.Client;

internal static class MemphisExceptions{

    public static MemphisConnectionException DeadConnectionException => new("Connection to the broker is dead");

    public static MemphisConnectionException UnableToDealyDLSException => new("Unable to delay DLS message");

    public static MemphisException EmptySchemaNameException => new("Schema name can not be empty");

    public static MemphisException SchemaNameTooLongException => new("Schema name should be under 128 characters");

    public static MemphisException InvalidSchemaNameException => new("Only alphanumeric and the '_', '-', '.' characters are allowed in schema name");

    public static MemphisException InvalidSchemaStartEndCharsException => new("Schema name can not start or end with non alphanumeric character");

    public static MemphisException EmptySchemaTypeException => new("Schema type can not be empty");

    public static MemphisException UnsupportedSchemaTypeException => new("Unsupported schema type, the supported schema types are: json, graphql, protobuf, avro\"");

    public static MemphisException SchemaUpdateSubscriptionFailedException => new("Unable to add subscription of schema updates for station");

    public static MemphisException InvalidConnectionTypeException => new("You have to connect with one of the following methods: connection token / password");

    public static MemphisException StationUnreachableException => new("Station unreachable");

    public static MemphisException BothPartitionNumAndKeyException => new("PartitionKey and PartitionNumber can not be set at the same time");

    public static MemphisMessageIdException EmptyMessageIDException => new("Message ID cannot be empty");

    public static MemphisException FailedToConnectException(System.Exception e)
    {
        return new MemphisConnectionException("error occurred, when connecting memphis", e);
    }

    public static MemphisException FailedToCreateConsumerException(System.Exception e)
    {
        return new MemphisException("Failed to create memphis consumer", e);
    }

    public static MemphisException FailedToCreateProducerException(System.Exception e)
    {
        return new MemphisException("Failed to create memphis producer", e);
    }


    public static MemphisException FailedToCreateStationException(System.Exception e)
    {
        return new MemphisException("Failed to create memphis station", e);
    }

    public static MemphisException FailedToAttachSchemaException(System.Exception e)
    {
        return new MemphisException("Failed to attach schema to station", e);
    }

    public static MemphisException SchemaDoesNotExistException(string path)
    {
        return new MemphisException("Schema file does not exist", new FileNotFoundException(path));
    }

    public static MemphisException FailedToDestroyConsumerException(System.Exception e)
    {
        return new MemphisException("Failed to destroy consumer", e);
    }

    public static MemphisConnectionException AckFailedException(System.Exception e)
    {
        return new MemphisConnectionException("Unable to ack message", e);
    }

    public static MemphisException NackFailedException(System.Exception e)
    {
        return new MemphisException("Unable to nack message", e);
    }

    public static MemphisException DeadLetterFailed(System.Exception e)
    {
        return new MemphisException("Unable to dead letter message", e);
    }
}
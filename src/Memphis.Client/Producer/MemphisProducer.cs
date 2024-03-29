using System.Linq;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using Memphis.Client.Station;

#pragma warning disable CS8602 // Possible null reference argument.

namespace Memphis.Client.Producer;

public sealed class MemphisProducer : IMemphisProducer
{
    internal string Key => $"{_internalStationName}_{_realName}";
    internal string InternalStationName { get => _internalStationName; }

    internal string StationName { get => _stationName; }
    internal string ProducerName { get => _producerName; }

    private readonly string _realName;
    private readonly string _producerName;
    private readonly string _stationName;
    private readonly string _internalStationName;
    private readonly MemphisClient _memphisClient;

    private readonly bool IsMultiStationProducer;
    private readonly List<string> _stations = new();

    internal StationPartitionResolver PartitionResolver { get; set; }

    public MemphisProducer(MemphisClient memphisClient, string producerName, string stationName, string realName)
    {
        _realName = realName ?? throw new ArgumentNullException(nameof(realName));
        _memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
        _producerName = producerName ?? throw new ArgumentNullException(nameof(producerName));
        _stationName = stationName ?? throw new ArgumentNullException(nameof(stationName));
        _internalStationName = MemphisUtil.GetInternalName(stationName);

        PartitionResolver = new(new int[0]);
        IsMultiStationProducer = false;
    }

    internal MemphisProducer(MemphisClient memphisClient, string producerName, string realName, List<string> stations)
    {
        _stationName = string.Empty;
        _internalStationName = string.Empty;
        PartitionResolver = new(new int[0]);

        _realName = realName ?? throw new ArgumentNullException(nameof(realName));
        _memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
        _producerName = producerName ?? throw new ArgumentNullException(nameof(producerName));

        if (stations == null || !stations.Any())
            throw new ArgumentNullException($"{nameof(stations)} cannot be null or empty");
        IsMultiStationProducer = true;
        _stations = stations;
    }

    /// <summary>
    /// Produce messages into station
    /// </summary>
    /// <param name="message">message to produce</param>
    /// <param name="headers">headers used to send data in the form of key and value</param>
    /// <param name="ackWaitMs">duration of time in milliseconds for acknowledgement</param>
    /// <param name="messageId">Message ID - for idempotent message production</param>
    /// <param name="asyncProduceAck">if true, the method will return immediately after sending the message to the broker. If false, the method will wait for the acknowledgement from the broker before returning.</param>
    /// <returns></returns>
    public async Task ProduceAsync(byte[] message, NameValueCollection headers, int ackWaitMs = 15_000,
        string? messageId = default, bool asyncProduceAck = true, string partitionKey = "", int partitionNumber = -1)
    {
        if (IsMultiStationProducer)
        {
            await MultiStationProduceAsync(message, headers, ackWaitMs, messageId, asyncProduceAck, partitionKey, partitionNumber);
            return;
        }

        await SingleStationProduceAsync(message, headers, ackWaitMs, messageId, asyncProduceAck, partitionKey, partitionNumber);
    }

    internal async Task SingleStationProduceAsync(
        byte[] message,
        NameValueCollection headers,
        int ackWaitMs,
        string? messageId,
        bool asyncProduceAck,
        string partitionKey,
        int partitionNumber)
    {
        await _memphisClient.ProduceAsync(this, message, headers, ackWaitMs, asyncProduceAck, messageId, partitionKey, partitionNumber);
    }

    internal async Task MultiStationProduceAsync(
        byte[] message,
        NameValueCollection headers,
        int ackWaitMs,
        string? messageId,
        bool asyncProduceAck,
        string partitionKey,
        int partitionNumber,
        CancellationToken cancellationToken = default)
    {
        foreach (var station in _stations)
        {
            var options = new MemphisProducerOptions
            {
                StationName = station,
                ProducerName = _producerName,
                MaxAckTimeMs = ackWaitMs,
            };

            await _memphisClient.ProduceAsync(
                options,
                message,
                headers,
                messageId,
                asyncProduceAck,
                partitionKey,
                partitionNumber,
                cancellationToken
            );
        }
    }


    /// <summary>
    /// Produce messages into station
    /// </summary>
    /// <param name="message">message to produce</param>
    /// <param name="headers">headers used to send data in the form of key and value</param>
    /// <param name="asyncProduceAck">if true, the method will return immediately after sending the message to the broker. If false, the method will wait for the acknowledgement from the broker before returning.</param>
    /// <param name="ackWaitMs">duration of time in milliseconds for acknowledgement</param>
    /// <param name="messageId">Message ID - for idempotent message production</param>
    /// <returns></returns>
    internal async Task ProduceToBrokerAsync(
        byte[] message,
        NameValueCollection headers,
        bool asyncProduceAck,
        string partitionKey,
        int partitionNumber,
        int ackWaitMs = 15_000,
        string? messageId = default)
    {
        await EnsureMessageIsValid(message, headers);

        string streamName = _internalStationName;
        if (_memphisClient.StationPartitions.TryGetValue(_internalStationName, out var partitions) &&
            partitions != null && partitions.PartitionsList != null)
        {
            if (partitions.PartitionsList.Length == 1)
            {
                streamName = $"{_internalStationName}${partitions.PartitionsList[0]}";
            }
            else if (partitions.PartitionsList.Length > 1)
            {
                if (partitionNumber > 0 && !string.IsNullOrWhiteSpace(partitionKey))
                    throw MemphisExceptions.BothPartitionNumAndKeyException;

                int partition = PartitionResolver.Resolve();

                if (!string.IsNullOrWhiteSpace(partitionKey))
                    partition = _memphisClient.GetPartitionFromKey(partitionKey, _internalStationName);

                else if (partitionNumber > 0)
                {
                    _memphisClient.EnsurePartitionNumberIsValid(partitionNumber, _internalStationName);
                    partition = partitionNumber;
                }

                streamName = $"{_internalStationName}${partition}";
            }
        }

        var msg = new Msg
        {
            Subject = FullSubjectName(),
            Data = message,
            Header = new MsgHeader
                {
                    {MemphisHeaders.MEMPHIS_PRODUCED_BY, _producerName},
                    {MemphisHeaders.MEMPHIS_CONNECTION_ID, _memphisClient.ConnectionId}
                }
        };

        if (messageId == string.Empty)
            throw MemphisExceptions.EmptyMessageIDException;

        if (!string.IsNullOrWhiteSpace(messageId))
        {
            msg.Header.Add(MemphisHeaders.MESSAGE_ID, messageId);
        }

        foreach (var headerKey in headers.AllKeys)
        {
            msg.Header.Add(headerKey, headers[headerKey]);
        }

        try
        {
            var publishOptions = PublishOptions.Builder()
                .WithTimeout(Duration.OfMillis(ackWaitMs))
                .Build();
            var publishAckTask = _memphisClient.PublishWithJetStreamAsync(msg, publishOptions);
            
            if (asyncProduceAck)
                return;

            var publishAck = await publishAckTask;
            if (publishAck.HasError)
            {
                throw new MemphisException(publishAck.ErrorDescription);
            }
        }
        catch (NATSNoRespondersException)
        {
            /// <summary>
            /// This exception is thrown when there are no station available to produce the message.
            /// The ReInitializeProducerAndRetry method will try to recreate the producer (which will also create the station) and retry to produce the message.
            /// </summary>
            await ReInitializeProducerAndRetry(message, headers, ackWaitMs, messageId);
        }
        catch (MemphisException)
        {
            throw;
        }
        catch (System.Exception ex)
        {
            throw new MemphisException(ex.Message);
        }

        async Task ReInitializeProducerAndRetry(byte[] message, NameValueCollection headers, int ackWaitMs = 15_000,
          string? messageId = default)
        {
            await _memphisClient.ProduceAsync(this, message, headers, ackWaitMs, asyncProduceAck, messageId, partitionKey);
        }

        string FullSubjectName()
        {
            if (_memphisClient.FunctionDetails.TryGetValue(_internalStationName, out var functionDetails))
            {
                string partitionString = streamName.Split('$')[1];
                int partitionNumber = Convert.ToInt32(partitionString);
                if (functionDetails.PartitionsFunctions.TryGetValue(partitionNumber, out var functionId))
                {
                    return $"{streamName}.functions.{functionId}";
                }
            }
            return $"{streamName}.final";
        }
    }


    /// <summary>
    /// Produce messages into station
    /// </summary>
    /// <param name="message">message to produce</param>
    /// <param name="headers">headers used to send data in the form of key and value</param>
    /// <param name="ackWaitMs">duration of time in milliseconds for acknowledgement</param>
    /// <param name="messageId">Message ID - for idempotent message production</param>
    /// <param name="asyncProduceAck">if true, the method will return immediately after sending the message to the broker. If false, the method will wait for the acknowledgement from the broker before returning.</param>
    /// <returns></returns>
    public async Task ProduceAsync<T>(T message, NameValueCollection headers, int ackWaitMs = 15_000,
        string? messageId = default, bool asyncProduceAck = true, string partitionKey = "", int partitionNumber = -1)
    {

        await ProduceAsync(
            SerializeMessage(message),
            headers,
            ackWaitMs,
            messageId,
            asyncProduceAck,
            partitionKey,
            partitionNumber);

        byte[] SerializeMessage(T message)
        {
            if (message is byte[])
                return (message as byte[])!;
            if (IsPrimitiveType(message))
                return Encoding.UTF8.GetBytes(message.ToString());
            var schemaType = _memphisClient.GetStationSchemaType(_internalStationName);
            return schemaType switch
            {
                MemphisSchemaTypes.JSON or
                MemphisSchemaTypes.GRAPH_QL or
                MemphisSchemaTypes.PROTO_BUF or
                MemphisSchemaTypes.AVRO => MessageSerializer.Serialize<object>(message!, schemaType),
                _ => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
            };
        }
    }

    /// <summary>
    /// Destroy producer
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MemphisException"></exception>
    public async Task DestroyAsync(int timeoutRetry = 5)
    {
        if (IsMultiStationProducer)
        {
            await DestroyMultiStationProducerAsync(timeoutRetry);
            return;
        }
        await DestroySingleStationProducerAsync(timeoutRetry);
    }


    public async Task DestroySingleStationProducerAsync(int timeoutRetry)
    {
        try
        {
            var removeProducerModel = new RemoveProducerRequest()
            {
                ProducerName = _producerName,
                StationName = _stationName,
                ConnectionId = _memphisClient.ConnectionId,
                Username = _memphisClient.Username,
                RequestVersion = MemphisRequestVersions.LastProducerDestroyRequestVersion,
            };

            var removeProducerModelJson = JsonSerializer.Serialize(removeProducerModel);

            byte[] removeProducerReqBytes = Encoding.UTF8.GetBytes(removeProducerModelJson);

            Msg removeProducerResp = await _memphisClient.RequestAsync(MemphisStations.MEMPHIS_PRODUCER_DESTRUCTIONS, removeProducerReqBytes, timeoutRetry);
            string errResp = Encoding.UTF8.GetString(removeProducerResp.Data);

            if (!string.IsNullOrEmpty(errResp))
            {
                throw new MemphisException(errResp);
            }

            await _memphisClient.NotifyRemoveProducer(_stationName);
        }
        catch (System.Exception e)
        {
            throw new MemphisException("Failed to destroy producer", e);
        }
    }

    private async Task DestroyMultiStationProducerAsync(int timeoutRetry)
    {
        var internalStationNames = _stations.Select(MemphisUtil.GetInternalName).ToList();
        var producerKeys = internalStationNames.Select(station => $"{station}_{_realName}").ToList();
        var producers = _memphisClient.ProducerCache.Where(producer => producerKeys.Contains(producer.Key)).Select(p => p.Value).ToList();

        foreach (var producer in producers)
            await producer.DestroyAsync(timeoutRetry);
    }

    /// <summary>
    /// Ensure message is valid
    /// </summary>
    /// <remark>
    /// This method is used to validate message against schema. If message is not valid, it will be sent to DLS, otherwise it will be sent to corresponding station
    /// </remark>
    /// <param name="message">Message to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    private async Task EnsureMessageIsValid(byte[] message, NameValueCollection headers, CancellationToken cancellationToken = default)
    {
        try
        {
            await _memphisClient.ValidateMessageAsync(message, _internalStationName, _producerName);
        }
        catch (MemphisSchemaValidationException exception)
        {
            await SendMessageToDls(message, headers, exception, cancellationToken);
            throw;
        }
        catch
        {
            throw;
        }
    }

    private async Task SendMessageToDls(byte[] message, NameValueCollection headers, MemphisSchemaValidationException validationError, CancellationToken cancellationToken = default)
    {
        if (!_memphisClient.IsSchemaVerseToDlsEnabled(_internalStationName))
            return;
        var headersForDls = new Dictionary<string, string>
        {
            [MemphisHeaders.MEMPHIS_CONNECTION_ID] = _memphisClient.ConnectionId,
            [MemphisHeaders.MEMPHIS_PRODUCED_BY] = _producerName
        };

        foreach (var headerKey in headers.AllKeys)
        {
            headersForDls.Add(headerKey, headers[headerKey]);
        }
        var dlsMessage = new DlsMessage
        {
            StationName = _internalStationName,
            Producer = new ProducerDetails
            {
                Name = _producerName,
                ConnectionId = _memphisClient.ConnectionId,
            },
            Message = new MessagePayloadDls
            {
                Data = BitConverter.ToString(message).Replace("-", string.Empty),
                Headers = headersForDls,
            },
            ValidationError = validationError.Message,
        };

        var dlsMessageJson = JsonSerializer.Serialize(dlsMessage);
        var dlsMessageBytes = Encoding.UTF8.GetBytes(dlsMessageJson);
        _memphisClient.BrokerConnection.Publish(MemphisSubjects.MEMPHIS_SCHEMA_VERSE_DLS, dlsMessageBytes);

        if (!_memphisClient.IsSendingNotificationEnabled)
            return;
        await _memphisClient.SendNotificationAsync(
            "Schema validation has failed",
            $"Schema validation has failed for station {_internalStationName} and producer {_producerName}. Error: {validationError.Message}",
            Encoding.UTF8.GetString(message),
            "schema_validation_fail_alert"
        );

    }

    /// <summary>
    /// Check if data is primitive type
    /// </summary>
    /// <typeparam name="T">Type of data</typeparam>
    /// <param name="data">Data to check</param>
    /// <returns>true if data type is primitive, otherwise false.</returns>
    private bool IsPrimitiveType<T>(T data)
    {
        var type = typeof(T);
        return type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(DateTime) ||
            type == typeof(decimal) ||
            type == typeof(string);
    }
}
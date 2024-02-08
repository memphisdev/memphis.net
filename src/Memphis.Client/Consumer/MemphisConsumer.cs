using Memphis.Client.Station;

namespace Memphis.Client.Consumer;

public sealed class MemphisConsumer : IMemphisConsumer
{
    public event EventHandler<MemphisMessageHandlerEventArgs> MessageReceived;
    public event EventHandler<MemphisMessageHandlerEventArgs> DlsMessageReceived;
    internal string InternalStationName { get; private set; }
    internal string Key => $"{InternalStationName}_{_consumerOptions.RealName}";
    private ISyncSubscription _dlsSubscription;
    private readonly MemphisClient _memphisClient;
    private readonly MemphisConsumerOptions _consumerOptions;
    private ConcurrentDictionary<int, IConsumerContext> _consumerContexts;
    private ConcurrentQueue<MemphisMessage> _dlsMessages;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _subscriptionActive;
    private readonly int _pingConsumerIntervalMs;
    /// <summary>
    /// Messages in DLS station will have a partition number of -1. This does not indicate the actual partition number of the message.
    /// Instead, it indicates that the message is in the DLS station.
    /// </summary>
    private const int DlsMessagePartitionNumber = -1;
    private int[] _partitions;
    internal StationPartitionResolver PartitionResolver { get; set; }
    internal int[] Partitions
    {
        get => _partitions;
        set
        {
            if (value.Length == 0)
            {
                PartitionResolver = new StationPartitionResolver(1);
                _partitions = value;
                return;
            }
            PartitionResolver = new StationPartitionResolver(value.Length);
            _partitions = value;
        }
    }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    public MemphisConsumer(MemphisClient memphisClient, MemphisConsumerOptions options, int[]? partitions = default)
    {
        if (options.StartConsumeFromSequence <= 0)
            throw new MemphisException($"Value of {nameof(options.StartConsumeFromSequence)} must be greater than 0");
        if (options.LastMessages < -1)
            throw new MemphisException($"Value of {nameof(options.LastMessages)} can not be less than -1");
        if (options is { StartConsumeFromSequence: > 1, LastMessages: > -1 })
            throw new MemphisException($"Consumer creation option can't contain both {nameof(options.StartConsumeFromSequence)} and {nameof(options.LastMessages)}");

        _memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
        _consumerOptions = options ?? throw new ArgumentNullException(nameof(options));
        InternalStationName = MemphisUtil.GetInternalName(options.StationName);
        _dlsMessages = new();

        _cancellationTokenSource = new();


        _subscriptionActive = true;
        _pingConsumerIntervalMs = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;

        Partitions = partitions ?? (new int[0]);

        InitConsumerContext();

#pragma warning disable 4014
        PingConsumer(_cancellationTokenSource.Token);
#pragma warning restore 4014

    }

    private void InitConsumerContext()
    {
        var durableName = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);
        var internalSubjectName = MemphisUtil.GetInternalName(_consumerOptions.StationName);

        if (Partitions.Length == 0)
        {
            var consumer = _memphisClient.GetConsumerContext(internalSubjectName, durableName);
            _consumerContexts = new ConcurrentDictionary<int, IConsumerContext>
            {
                [1] = consumer
            };
            return;
        }

        _consumerContexts = new ConcurrentDictionary<int, IConsumerContext>();
        for (int i = 0; i < Partitions.Length; i++)
        {
            var consumerStreamName = $"{internalSubjectName}${Partitions[i]}";
            var consumer = _memphisClient.GetConsumerContext(consumerStreamName, durableName);
            _consumerContexts.AddOrUpdate(Partitions[i], consumer, (_, _) => consumer);
        }
    }

    /// <summary>
    /// ConsumeAsync messages
    /// </summary>
    /// <returns></returns>
    public Task ConsumeAsync(CancellationToken cancellationToken = default)
    {
        return ConsumeAsync(new(), cancellationToken);
    }

    /// <summary>
    /// ConsumeAsync messages
    /// </summary>
    /// <param name="options">Consume options</param>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns></returns>
    public Task ConsumeAsync(ConsumeOptions options, CancellationToken cancellationToken = default)
    {
        var consumeTask = Task.Factory.StartNew(() =>
            Consume(options.PartitionKey, options.PartitionNumber, cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        var consumeFromDlsTask = Task.Factory.StartNew(() =>
            ConsumeFromDls(cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        return Task.WhenAll(consumeTask, consumeFromDlsTask);
    }

    /// <summary>
    /// Destroy the consumer
    /// </summary>
    /// <returns></returns>
    public async Task DestroyAsync(int timeoutRetry = 5)
    {
        try
        {
            if (_dlsSubscription is { IsValid: true })
            {
                await _dlsSubscription.DrainAsync();
            }

            StopConsume();

            _cancellationTokenSource?.Cancel();

            var removeConsumerModel = new RemoveConsumerRequest()
            {
                ConsumerName = _consumerOptions.ConsumerName,
                StationName = _consumerOptions.StationName,
                ConnectionId = _memphisClient.ConnectionId,
                Username = _memphisClient.Username,
                RequestVersion = MemphisRequestVersions.LastConsumerDestroyRequestVersion,
            };

            var removeConsumerModelJson = JsonSerializer.Serialize(removeConsumerModel);

            byte[] removeConsumerReqBytes = Encoding.UTF8.GetBytes(removeConsumerModelJson);

            Msg removeProducerResp = await _memphisClient.RequestAsync(MemphisStations.MEMPHIS_CONSUMER_DESTRUCTIONS, removeConsumerReqBytes, timeoutRetry);
            string errResp = Encoding.UTF8.GetString(removeProducerResp.Data);

            if (!string.IsNullOrEmpty(errResp))
            {
                throw new MemphisException(errResp);
            }

            await _memphisClient.NotifyRemoveConsumer(_consumerOptions.StationName);
        }
        catch (System.Exception e)
        {
            throw MemphisExceptions.FailedToDestroyConsumerException(e);
        }
    }

    /// <summary>
    /// Fetch a batch of messages
    /// </summary>
    /// <param name="batchSize">the number of messages to fetch</param>
    /// <param name="prefetch">if true, Memphis will prefetch messages for the consumer</param>
    /// <returns>A batch of messages</returns>
    public IEnumerable<MemphisMessage> Fetch(int batchSize, bool prefetch)
    {
        return Fetch(new()
        {
            BatchSize = batchSize,
            Prefetch = prefetch,
        });
    }

    public async Task<IEnumerable<MemphisMessage>> FetchMessages(
        FetchMessageOptions options,
        CancellationToken cancellationToken = default
    )
    {
        return await Task.Run(() => Fetch(options), cancellationToken);
    }

    internal IEnumerable<MemphisMessage> Fetch(FetchMessageOptions fetchMessageOptions)
    {
        MemphisClient.EnsureBatchSizeIsValid(fetchMessageOptions.BatchSize);
        try
        {
            var batchSize = fetchMessageOptions.BatchSize;
            _consumerOptions.BatchSize = batchSize;
            IEnumerable<MemphisMessage> messages = Enumerable.Empty<MemphisMessage>();

            int dlsMessageCount = _dlsMessages.Count();
            if (dlsMessageCount > 0)
            {
                if (dlsMessageCount <= batchSize)
                {
                    messages = _dlsMessages.ToList();
                    _dlsMessages = new();
                }
                else
                {
                    DequeueDlsMessages(batchSize, ref messages);
                }
                return messages;
            }

            if (TryGetAndRemovePrefetchedMessages(batchSize, out IEnumerable<MemphisMessage> prefetchedMessages))
            {
                messages = prefetchedMessages;
            }

            if (fetchMessageOptions.Prefetch)
            {
                Task.Run(() => Prefetch(fetchMessageOptions.PartitionKey, fetchMessageOptions.PartitionNumber), _cancellationTokenSource.Token);
            }

            if (messages.Any())
            {
                return messages;
            }

            return FetchSubscriptionWithTimeOut(fetchMessageOptions.PartitionKey, fetchMessageOptions.PartitionNumber);
        }
        catch (System.Exception ex)
        {
            throw new MemphisException(ex.Message, ex);
        }
    }

    internal bool TryGetAndRemovePrefetchedMessages(int batchSize, out IEnumerable<MemphisMessage> messages)
    {
        messages = Enumerable.Empty<MemphisMessage>();
        var lowerCaseStationName = _consumerOptions.StationName.ToLower();
        var consumerGroup = _consumerOptions.ConsumerGroup;
        if (!_memphisClient.PrefetchedMessages.ContainsKey(lowerCaseStationName))
            return false;
        if (!_memphisClient.PrefetchedMessages[lowerCaseStationName].ContainsKey(consumerGroup))
            return false;
        if (!_memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup].Any())
            return false;
        var prefetchedMessages = _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup];
        if (prefetchedMessages.Count <= batchSize)
        {
            messages = prefetchedMessages;
            _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup] = new();
            return true;
        }
        messages = prefetchedMessages.Take(batchSize);
        _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup] = prefetchedMessages
            .Skip(batchSize)
            .ToList();
        return true;
    }

    internal void Prefetch(string partitionKey, int consumerPartitionNumber)
    {
        var lowerCaseStationName = _consumerOptions.StationName.ToLower();
        var consumerGroup = _consumerOptions.ConsumerGroup;
        if (!_memphisClient.PrefetchedMessages.ContainsKey(lowerCaseStationName))
        {
            _memphisClient.PrefetchedMessages[lowerCaseStationName] = new();
        }
        if (!_memphisClient.PrefetchedMessages[lowerCaseStationName].ContainsKey(consumerGroup))
        {
            _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup] = new();
        }
        var messages = FetchSubscriptionWithTimeOut(partitionKey, consumerPartitionNumber);
        _memphisClient.PrefetchedMessages[lowerCaseStationName][consumerGroup].AddRange(messages);
    }

    /// <summary>
    /// Pings Memphis every 30 seconds to check if the consumer is still alive.
    /// If Memphis does not respond, the consumer is stopped.
    /// </summary>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns></returns>
    internal async Task PingConsumer(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {

            foreach (var consumerContext in _consumerContexts.Values)
            {
                try
                {
                    _ = consumerContext.GetConsumerInfo();
                }
                catch (System.Exception exception) when (IsConsumerOrStreamNotFound(exception))
                {
                    MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(
                        new List<MemphisMessage>(),
                        consumerContext,
                        MemphisExceptions.StationUnreachableException));

                    _subscriptionActive = false;
                }
                catch { }
            }

            await Task.Delay(_pingConsumerIntervalMs, cancellationToken);
        }

        static bool IsConsumerOrStreamNotFound(System.Exception exception)
        {
            if (exception is null || string.IsNullOrWhiteSpace(exception.Message))
            {
                return false;
            }
            return exception.Message.Contains("consumer not found") ||
                   exception.Message.Contains("stream not found");
        }
    }

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    internal void StopConsume()
    {
        _subscriptionActive = false;
    }

    private IEnumerable<MemphisMessage> FetchSubscriptionWithTimeOut(string partitionKey, int consumerPartitionNumber)
    {
        int partitionNumber = Partitions.Length == 0 ? 0 : PartitionResolver.Resolve();

        if (!string.IsNullOrWhiteSpace(partitionKey) && consumerPartitionNumber > 0)
        {
            throw MemphisExceptions.BothPartitionNumAndKeyException;
        }
        if (!string.IsNullOrWhiteSpace(partitionKey))
        {
            partitionNumber = _memphisClient.GetPartitionFromKey(partitionKey, _consumerOptions.StationName);
        }
        else if (consumerPartitionNumber > 0)
        {
            _memphisClient.EnsurePartitionNumberIsValid(consumerPartitionNumber, _consumerOptions.StationName);
            partitionNumber = consumerPartitionNumber;
        }

        _consumerContexts.TryGetValue(partitionNumber, out IConsumerContext consumer);
            
        return consumer.FetchMessages(new FetchOptions(
            _memphisClient,
            InternalStationName,
            _consumerOptions,
            consumerPartitionNumber
        ));
    }

    /// <summary>
    /// ConsumeAsync messages from station
    /// </summary>
    /// <param name="msgCallbackHandler">the event handler for messages consumed from station in which MemphisConsumer created for</param>
    /// <param name="cancellationToken">token used to cancel operation by Consumer</param>
    /// <returns></returns>
    private async Task Consume(
        string partitionKey,
        int partitionNumber,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_subscriptionActive)
            {
                break;
            }

            FetchFromPartition(partitionKey, partitionNumber, cancellationToken);
            await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
        }
    }

    private void FetchFromPartition(
        string partitionKey,
        int consumerPartitionNumber,
        CancellationToken cancellationToken
    )
    {
        var partitionNumber = 1;
        if (_consumerContexts is { Count: > 1 })
        {
            if (!string.IsNullOrWhiteSpace(partitionKey) && consumerPartitionNumber > 0)
            {
                throw MemphisExceptions.BothPartitionNumAndKeyException;
            }
            if (!string.IsNullOrWhiteSpace(partitionKey))
            {
                partitionNumber = _memphisClient.GetPartitionFromKey(partitionKey, InternalStationName);
            }
            else if (consumerPartitionNumber > 0)
            {
                _memphisClient.EnsurePartitionNumberIsValid(consumerPartitionNumber, InternalStationName);
                partitionNumber = consumerPartitionNumber;
            }
            else
            {
                partitionNumber = PartitionResolver.Resolve();
            }
        }
        _consumerContexts.TryGetValue(partitionNumber, out IConsumerContext consumerContext);
        try
        {
            var memphisMessages = consumerContext.FetchMessages(new FetchOptions(
                _memphisClient,
                InternalStationName,
                _consumerOptions,
                partitionNumber
            ));

            if (memphisMessages is null || !memphisMessages.Any())
                return;

            MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(memphisMessages, consumerContext, null));
        }
        catch (System.Exception e)
        {
            MessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), consumerContext, e));
        }
    }

    /// <summary>
    /// ConsumeAsync messages from dead letter queue namely, DLS
    /// </summary>
    /// <returns></returns>
    private async Task ConsumeFromDls(CancellationToken cancellationToken)
    {
        var subjectToConsume = MemphisUtil.GetInternalName(_consumerOptions.StationName);
        var consumerGroup = MemphisUtil.GetInternalName(_consumerOptions.ConsumerGroup);

        var dlsSubscriptionName = MemphisSubscriptions.DLS_PREFIX + subjectToConsume + "." + consumerGroup;
        _dlsSubscription = _memphisClient.BrokerConnection.SubscribeSync(dlsSubscriptionName, dlsSubscriptionName);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsSubscriptionActive(_dlsSubscription))
                continue;

            if (!_subscriptionActive)
            {
                break;
            }

            try
            {
                var msg = _dlsSubscription.NextMessage();
                if (msg is null)
                {
                    continue;
                }

                MemphisMessage memphisMsg = new(
                    msg,
                    _memphisClient,
                    _consumerOptions.ConsumerGroup,
                    _consumerOptions.MaxAckTimeMs,
                    InternalStationName,
                    DlsMessagePartitionNumber
                );
                if (DlsMessageReceived is null)
                {
                    EnqueueDlsMessage(memphisMsg);
                    continue;
                }

                var memphisMessageList = new List<MemphisMessage> { memphisMsg };
                IConsumerContext consumerContext;
                if(Partitions.Length == 0)
                {
                    _consumerContexts.TryGetValue(0, out consumerContext);
                }
                else
                {
                    _consumerContexts.TryGetValue(PartitionResolver.Resolve(), out consumerContext);
                }
                DlsMessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(memphisMessageList, consumerContext, null));

                await Task.Delay(_consumerOptions.PullIntervalMs, cancellationToken);
            }
            catch (System.Exception e)
            {
                IConsumerContext consumerContext;
                if(Partitions.Length == 0)
                {
                    _consumerContexts.TryGetValue(0, out consumerContext);
                }
                else
                {
                    _consumerContexts.TryGetValue(PartitionResolver.Resolve(), out consumerContext);
                }

                DlsMessageReceived?.Invoke(this, new MemphisMessageHandlerEventArgs(new List<MemphisMessage>(), consumerContext, e));
            }

        }
    }

    private bool IsSubscriptionActive(ISyncSubscription subscription)
    {
        return
            subscription.IsValid &&
            subscription.Connection.State != ConnState.CLOSED;
    }

    private void EnqueueDlsMessage(MemphisMessage message)
    {
        int insertToIndex = _dlsMessages.Count();
        if (insertToIndex > 10_000)
        {
            _dlsMessages.TryDequeue(out MemphisMessage _);
        }
        _dlsMessages.Enqueue(message);
    }

    private void DequeueDlsMessages(int batchSize, ref IEnumerable<MemphisMessage> messages)
    {
        if (messages is not { })
            messages = Enumerable.Empty<MemphisMessage>();
        List<MemphisMessage> batchMessages = new();
        while (_dlsMessages.TryDequeue(out MemphisMessage message))
        {
            batchSize -= 1;
            batchMessages.Add(message);
            if (batchSize <= 0)
                break;
        }
        messages.Concat(batchMessages);
    }

    public async void Dispose()
    {
        StopConsume();
        await _dlsSubscription.DrainAsync();

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        _memphisClient?.Dispose();
        _dlsSubscription?.Dispose();
    }
}
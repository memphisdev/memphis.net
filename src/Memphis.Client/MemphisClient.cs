using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Memphis.Client.Constants;
using Memphis.Client.Consumer;
using Memphis.Client.Exception;
using Memphis.Client.Helper;
using Memphis.Client.Models.Request;
using Memphis.Client.Models.Response;
using Memphis.Client.Producer;
using NATS.Client;
using NATS.Client.JetStream;

namespace Memphis.Client
{
    public class MemphisClient : IDisposable
    {
        //TODO replace it with mature solution
        private bool _connectionActive;

        private readonly Options _brokerConnOptions;
        private readonly IConnection _brokerConnection;
        private readonly IJetStream _jetStreamContext;
        private readonly string _connectionId;

        private Dictionary<string, string> _schemaUpdateData = new Dictionary<string, string>();
        private CancellationTokenSource _cancellationTokenSource;

        private readonly ConcurrentDictionary<string, ProducerSchemaUpdateInit> _schemaUpdateDictionary;
        private readonly ConcurrentDictionary<string, object> _subscriptionPerSchema;

        private readonly ConcurrentDictionary<string, int> _producerPerStations;

        public MemphisClient(Options brokerConnOptions, IConnection brokerConnection,
            IJetStream jetStreamContext, string connectionId)
        {
            this._brokerConnOptions = brokerConnOptions ?? throw new ArgumentNullException(nameof(brokerConnOptions));
            this._brokerConnection = brokerConnection ?? throw new ArgumentNullException(nameof(brokerConnection));
            this._jetStreamContext = jetStreamContext ?? throw new ArgumentNullException(nameof(jetStreamContext));
            this._connectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));

            //TODO need to handle mechanism to check connection being active throughout client is being used
            this._connectionActive = true;
            this._cancellationTokenSource = new CancellationTokenSource();

            this._schemaUpdateDictionary = new ConcurrentDictionary<string, ProducerSchemaUpdateInit>();
            this._subscriptionPerSchema = new ConcurrentDictionary<string, object>();
            this._producerPerStations = new ConcurrentDictionary<string, int>();
        }


        /// <summary>
        /// Create Producer for station 
        /// </summary>
        /// <param name="stationName">name of station which producer will produce data to</param>
        /// <param name="producerName">name of producer which used to define uniquely</param>
        /// <param name="generateRandomSuffix">feature flag based param used to add randomly generated suffix for producer's name</param>
        /// <returns>An <see cref="MemphisProducer"/> object connected to the station to produce data</returns>
        public async Task<MemphisProducer> CreateProducer(string stationName, string producerName,
            bool generateRandomSuffix = false)
        {
            if (!_connectionActive)
            {
                throw new MemphisConnectionException("Connection is dead");
            }

            if (generateRandomSuffix)
            {
                producerName = $"{producerName}_{MemphisUtil.GetUniqueKey(8)}";
            }

            try
            {
                var createProducerModel = new CreateProducerRequest
                {
                    ProducerName = producerName,
                    StationName = MemphisUtil.GetInternalName(stationName),
                    ConnectionId = _connectionId,
                    ProducerType = "application",
                    RequestVersion = 1
                };

                var createProducerModelJson = JsonSerDes.PrepareJsonString<CreateProducerRequest>(createProducerModel);

                byte[] createProducerReqBytes = Encoding.UTF8.GetBytes(createProducerModelJson);

                Msg createProducerResp = await _brokerConnection.RequestAsync(
                    MemphisStations.MEMPHIS_PRODUCER_CREATIONS, createProducerReqBytes);
                string respAsJson = Encoding.UTF8.GetString(createProducerResp.Data);
                var respAsObject =
                    (CreateProducerResponse) JsonSerDes.PrepareObjectFromString<CreateProducerResponse>(respAsJson);

                if (!string.IsNullOrEmpty(respAsObject.Error))
                {
                    throw new MemphisException(respAsObject.Error);
                }

                string internalStationName = MemphisUtil.GetInternalName(stationName);

                await this.listenForSchemaUpdate(internalStationName, respAsObject.SchemaUpdate);

                if (_schemaUpdateData.TryGetValue(internalStationName, out string schemaForStation))
                {
                    //TODO if schema data is protoBuf then parse its descriptors
                    //self.schema_updates_data[station_name_internal]['type'] == "protobuf"
                    // elf.parse_descriptor(station_name_internal)
                }

                return new MemphisProducer(this, producerName, stationName);
            }
            catch (System.Exception e)
            {
                throw new MemphisException("Failed to create memphis producer", e);
            }
        }

        /// <summary>
        /// Create Consumer for station 
        /// </summary>
        /// <param name="consumerOptions">options used to customize the behaviour of consumer</param>
        /// <returns>An <see cref="MemphisConsumer"/> object connected to the station from consuming data</returns>
        public async Task<MemphisConsumer> CreateConsumer(ConsumerOptions consumerOptions)
        {
            if (!_connectionActive)
            {
                throw new MemphisConnectionException("Connection is dead");
            }

            if (consumerOptions.GenerateRandomSuffix)
            {
                consumerOptions.ConsumerName = $"{consumerOptions.ConsumerName}_{MemphisUtil.GetUniqueKey(8)}";
            }

            if (string.IsNullOrEmpty(consumerOptions.ConsumerGroup))
            {
                consumerOptions.ConsumerGroup = consumerOptions.ConsumerName;
            }

            try
            {
                var createConsumerModel = new CreateConsumerRequest
                {
                    ConsumerName = consumerOptions.ConsumerName,
                    StationName = consumerOptions.StationName,
                    ConnectionId = _connectionId,
                    ConsumerType = "application",
                    ConsumerGroup = consumerOptions.ConsumerGroup,
                    MaxAckTimeMs = consumerOptions.MaxAckTimeMs,
                    MaxMsgCountForDelivery = consumerOptions.MaxMsdgDeliveries,
                };

                var createConsumerModelJson = JsonSerDes.PrepareJsonString<CreateConsumerRequest>(createConsumerModel);

                byte[] createConsumerReqBytes = Encoding.UTF8.GetBytes(createConsumerModelJson);

                Msg createConsumerResp = await _brokerConnection.RequestAsync(
                    MemphisStations.MEMPHIS_CONSUMER_CREATIONS, createConsumerReqBytes);
                string respAsJson = Encoding.UTF8.GetString(createConsumerResp.Data);

                if (!string.IsNullOrEmpty(respAsJson))
                {
                    throw new MemphisException(respAsJson);
                }

                return new MemphisConsumer(this, consumerOptions);
            }
            catch (System.Exception e)
            {
                throw new MemphisException("Failed to create memphis producer", e);
            }
        }


        private async Task listenForSchemaUpdate(string internalStationName, ProducerSchemaUpdateInit schemaUpdateInit)
        {
            var schemaUpdateSubject = MemphisSubjects.MEMPHIS_SCHEMA_UPDATE + internalStationName;

            if (!string.IsNullOrEmpty(schemaUpdateInit.SchemaName))
            {
                _schemaUpdateDictionary.TryAdd(internalStationName, schemaUpdateInit);
            }


            if (_subscriptionPerSchema.TryGetValue(internalStationName, out object schemaSub))
            {
                _producerPerStations.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
                return;
            }

            var subscription = _brokerConnection.SubscribeSync(schemaUpdateSubject);

            if (!_subscriptionPerSchema.TryAdd(internalStationName, subscription))
            {
                throw new MemphisException("Unable to add subscription of schema updates for station");
            }

            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var schemaUpdateMsg = subscription.NextMessage();
                    await processAndStoreSchemaUpdate(internalStationName, schemaUpdateMsg);
                }
            }, _cancellationTokenSource.Token);

            _producerPerStations.AddOrUpdate(internalStationName, 1, (key, val) => val + 1);
        }


        private async Task processAndStoreSchemaUpdate(string internalStationName, Msg message)
        {
            string respAsJson = Encoding.UTF8.GetString(message.Data);
            var respAsObject =
                (ProducerSchemaUpdate) JsonSerDes.PrepareObjectFromString<ProducerSchemaUpdate>(respAsJson);

            if (!string.IsNullOrEmpty(respAsObject?.Init?.SchemaName))
            {
                if (!this._schemaUpdateDictionary.TryAdd(internalStationName, respAsObject.Init))
                {
                    throw new MemphisException("Unable to save schema data for station");
                }
                //TODO add parse descriptor
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _brokerConnection.Dispose();
            _connectionActive = false;
        }

        internal IConnection BrokerConnection
        {
            get { return _brokerConnection; }
        }

        internal IJetStream JetStreamConnection
        {
            get { return _jetStreamContext; }
        }

        internal string ConnectionId
        {
            get { return _connectionId; }
        }

        internal bool ConnectionActive
        {
            get { return _connectionActive; }
        }
    }
}
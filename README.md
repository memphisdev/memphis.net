<a href="![Github (4)](https://github.com/memphisdev/memphis-terraform/assets/107035359/a5fe5d0f-22e1-4445-957d-5ce4464e61b1)">[![Github (4)](https://github.com/memphisdev/memphis-terraform/assets/107035359/a5fe5d0f-22e1-4445-957d-5ce4464e61b1)](https://memphis.dev)</a>
<p align="center">
<a href="https://memphis.dev/discord"><img src="https://img.shields.io/discord/963333392844328961?color=6557ff&label=discord" alt="Discord"></a>
<a href="https://github.com/memphisdev/memphis/issues?q=is%3Aissue+is%3Aclosed"><img src="https://img.shields.io/github/issues-closed/memphisdev/memphis?color=6557ff"></a> 
  <img src="https://img.shields.io/npm/dw/memphis-dev?color=ffc633&label=installations">
<a href="https://github.com/memphisdev/memphis/blob/master/CODE_OF_CONDUCT.md"><img src="https://img.shields.io/badge/Code%20of%20Conduct-v1.0-ff69b4.svg?color=ffc633" alt="Code Of Conduct"></a> 
<img alt="GitHub release (latest by date)" src="https://img.shields.io/github/v/release/memphisdev/memphis?color=61dfc6">
<img src="https://img.shields.io/github/last-commit/memphisdev/memphis?color=61dfc6&label=last%20commit">
</p>

<div align="center">
  
<img width="177" alt="cloud_native 2 (5)" src="https://github.com/memphisdev/memphis/assets/107035359/a20ea11c-d509-42bb-a46c-e388c8424101"> 

  <h4>
    
**[Memphis.dev](https://memphis.dev)** is a highly scalable, painless, and effortless data streaming platform.<br>
Made to enable developers and data teams to collaborate and build<br>
real-time and streaming apps fast.

  </h4>
  
</div>

## Installation

```sh
 dotnet add package Memphis.Client -v ${MEMPHIS_CLIENT_VERSION}
```

## Update

```sh
Update-Package Memphis.Client
```

## Importing

```c#
using Memphis.Client;
```
First, a connection to Memphis must be made:

```c#
using Memphis.Client;

// Connecting to the broker
var options = MemphisClientFactory.GetDefaultOptions();
options.Host = "<memphis-host>";
options.AccountId = <memphis-accountid>;
options.Username = "<memphis-username>";
options.Password = "<memphis-password>";

var memphisClient = await MemphisClientFactory.CreateClient(options);
```

Then, to produce a message, call the `memphisClient.ProduceAsync` function or create a producer and call its `producer.ProduceAsync` function:

```C#
Message message = new()
{
    Hello = "World!"
};

var headers = new NameValueCollection();

var msgBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
await memphisClient.ProduceAsync(new Memphis.Client.Producer.MemphisProducerOptions
    {
        StationName = "<station-name>",
        ProducerName = "<producer-name>"
    },
    msgBytes,
    headers);

public class Message
{
    public string Hello { get; set; }
}

memphisClient.Dispose();
```

Lastly, to consume this message, call the `memphisClient.FetchMessages` function or create a consumer and call its `consumer.Fetch` function:

```C#
var messages = await memphisClient.FetchMessages(new Memphis.Client.Consumer.FetchMessageOptions
    {
        StationName = "<station-name>",
        ConsumerName = "<consumer-name>",
        Prefetch = false
    });

foreach (MemphisMessage message in messages)
{
    var messageData = message.GetData();
    var messageOBJ = JsonSerializer.Deserialize<Message>(messageData);

    // Do something with the message object here

    message.Ack();
}

memphisClient.Dispose();
```

> Remember to call `memphisClient.Dispose()` to close the connection!


### Connecting to Memphis

The createClient method in the Memphis class allows for the connection to Memphis. Connecting to Memphis (cloud or open-source) will be needed in order to use any of the other functionality of the Memphis class. Upon connection, all of Memphis' features are available.

First, we need to create or use default `ClientOptions` and then connect to Memphis by using `MemphisClientFactory.CreateClient(ClientOptions opts)`.

```c#
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<broker-address>";
    options.Username = "<username>";
    options.ConnectionToken = "<broker-token>"; // you will get it on broker creation
    options.AccountId = <account-id>; // You can find it on the profile page in the Memphis UI. This field should be set only on the cloud version of Memphis, otherwise it will be ignored
    options.MaxReconnect = <max-reconnect>; // Gets or sets the maximum number of times connection will attempt to reconnect. To reconnect indefinitely set this value to -1. The default value is -1.
    var memphisClient = await MemphisClientFactory.CreateClient(options);
    ...
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

A password-based connection would look like this (using the defualt root memphis login with Memphis open-source):

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";  
    var memphisClient = await MemphisClientFactory.CreateClient(options);
}
catch (Exception ex) {
    // handle exception
}
```

If you wanted to connect to Memphis cloud instead, simply add your account ID and change the host. The host and account_id can be found on the Overview page in the Memphis cloud UI under your name at the top. Here is an example to connecting to a cloud broker that is located in US East:  

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "aws-us-east-1.cloud.memphis.dev";
    options.Username = "my_client_username";
    options.Password = "my_client_password";  
    options.AccountId = 123456789;
    var memphisClient = await MemphisClientFactory.CreateClient(options);
}
catch (Exception ex) {
    // handle exception
}
```

It is possible to use a token-based connection to memphis as well, where multiple users can share the same token to connect to memphis. Here is an example of using memphis.connect with a token:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.ConnectionToken = "Token";  
    var memphisClient = await MemphisClientFactory.CreateClient(options);
}
catch (Exception ex) {
    // handle exception
}
```

The token will be presented when creating new users. 

Memphis needs to be configured to use token based connection. See the [docs](https://docs.memphis.dev/memphis/memphis-broker/concepts/security) for help doing this.

A TLS based connection would look like this:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Tls = new TlsOptions("tlsFileName");
    var memphisClient = await MemphisClientFactory.CreateClient(options);
}
catch (Exception ex)
{
    // handle exception
}
```

Memphis needs to configured for these use cases. To configure memphis to use TLS see the [docs](https://docs.memphis.dev/memphis/open-source-installation/kubernetes/production-best-practices#memphis-metadata-tls-connection-configuration). 

### Disconnecting from Memphis

To disconnect from Memphis, call `Dispose()` on the `MemphisClient`.

```c#
memphisClient.Dispose()
```
### Creating a Station

Stations are distributed units that store messages. Producers add messages to stations and Consumers take messages from them. Each station stores messages until their retention policy causes them to either delete the messages or move them to [remote storage](https://docs.memphis.dev/memphis/integrations-center/storage/s3-compatible). 

**A station will be automatically created for the user when a consumer or producer is used if no stations with the given station name exist.**<br><br>
_If the station trying to be created exists when this function is called, nothing will change with the exisitng station_

```c#
try
{
    // First: creating Memphis client
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.Username = "<username>";
    options.Password = "<password>";
    options.AccountId = <account-id>; // You can find it on the profile page in the Memphis UI. This field should be set only on the cloud version of Memphis, otherwise it will be ignored
    var client = await MemphisClientFactory.CreateClient(options);
    
    // Second: creaing Memphis station
    var station = await client.CreateStation(
        stationOptions: new StationOptions()
        {
            Name = "<station-name>",
            RetentionType = RetentionTypes.MAX_MESSAGE_AGE_SECONDS,
            RetentionValue = 3600,
            StorageType = StorageTypes.DISK,
            Replicas = 1,
            IdempotencyWindowMs = 0,
            SendPoisonMessageToDls = true,
            SendSchemaFailedMessageToDls = true,
            PartitionsNumber = 3, // defaults to 1
            DlsStation = "<dls-station>" // If DlsStation is set, then DLS events will be sent to selected station as well. The default value is "" (no DLS station).
        });
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

The CreateStation method is used to create a station. Using the different options available, one can programically create many different types of stations. The Memphis UI can also be used to create stations to the same effect. 

A minimal example, using all default values would simply create a station with the given name:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation"
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

To change what criteria the station uses to decide if a message should be retained in the station, change the retention type. The different types of retention are documented [here](https://github.com/memphisdev/memphis.net#retention-types) in the dotnet README. 

The unit of the rentention value will vary depending on the RetentionTypes. The [previous link](https://github.com/memphisdev/memphis.net#retention-types) also describes what units will be used. 

Here is an example of a station which will only hold up to 10 messages:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            RetentionType = RetentionTypes.MESSAGES,
            RetentionValue = 10
        }
    );  
}
catch (Exception ex)
{
    // handle exception
}
```

Memphis stations can either store Messages on disk or in memory. A comparison of those types of storage can be found [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/storage-and-redundancy#tier-1-local-storage).

Here is an example of how to create a station that uses Memory as its storage type:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            StorageType = StorageTypes.MEMORY
        }
    );
}
catch (Exception ex) {
    // handle exception
}
```

In order to make a station more redundant, replicas can be used. Read more about replicas [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/storage-and-redundancy#replicas-mirroring). Note that replicas are only available in cluster mode. Cluster mode can be enabled in the [Helm settings](https://docs.memphis.dev/memphis/open-source-installation/kubernetes/1-installation#appendix-b-helm-deployment-options) when deploying Memphis with Kubernetes.

Here is an example of creating a station with 3 replicas:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            Replicas = 3
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

Idempotency defines how Memphis will prevent duplicate messages from being stored or consumed. The duration of time the message ID's will be stored in the station can be set with IdempotencyWindowMs. If the environment Memphis is deployed in has unreliably connection and/or a lot of latency, increasing this value might be desiriable. The default duration of time is set to two minutes. Read more about idempotency [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/idempotency).

Here is an example of changing the idempotency window to 3 seconds:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            IdempotenceWindowMs = 180_000
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

The schema name is used to set a schema to be enforced by the station. The default value of "" ensures that no schema is enforced. Here is an example of changing the schema to a defined schema in schemaverse called "SensorLogs":

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            SchemaName = "SensorLogs"
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

There are two options for sending messages to the [dead-letter station(DLS)](https://docs.memphis.dev/memphis/memphis-broker/concepts/dead-letter#terminology). These are SendPoisonMessageToDls and SendSchemaFailedMessageToDls. 

Here is an example of sending poison messages to the DLS but not messages which fail to conform to the given schema.

```csharp
    try
    {
        var options = MemphisClientFactory.GetDefaultOptions();
        options.Host = "localhost";
        options.Username = "root";
        options.Password = "memphis";
        var memphisClient = await MemphisClientFactory.CreateClient(options);

        var station = await memphisClient.CreateStation(
            stationOptions: new StationOptions
            {
                Name = "MyNewStation",
                SendPoisonMessageToDls = true,
                SendSchemaFailedMessageToDls = false
            }
        );
    }
    catch (Exception ex)
    {
        // handle exception
    }
```

When either of the DLS flags are set to True, a station can also be set to handle these events. To set a station as the station to where schema failed or poison messages will be set to, use the DlsStation StationOptions:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            SendPoisonMessageToDls = true,
            SendSchemaFailedMessageToDls = false,
            // DlsStation = "DeadLetterMessageStation" // Coming soon
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

When the retention value is met, Mempihs by default will delete old messages. If tiered storage is setup, Memphis can instead move messages to tier 2 storage. Read more about tiered storage [here](https://docs.memphis.dev/memphis/memphis-broker/concepts/storage-and-redundancy#storage-tiering). Enable this setting with the option provided:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            TieredStorageEnabled = true
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```

[Partitioning](https://docs.memphis.dev/memphis/memphis-broker/concepts/station#partitions) might be useful for a station. To have a station partitioned, simply change the partitions number:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var station = await memphisClient.CreateStation(
        stationOptions: new StationOptions
        {
            Name = "MyNewStation",
            PartitionsNumber = 3
        }
    );
}
catch (Exception ex)
{
    // handle exception
}
```


### Retention types

Retention types define the methodology behind how a station behaves with its messages. Memphis currently supports the following retention types:

```c#
RetentionTypes.MAX_MESSAGE_AGE_SECONDS
```
When the retention type is set to MAX_MESSAGE_AGE_SECONDS, messages will persist in the station for the number of seconds specified in the retention_value. 

```c#
RetentionTypes.MESSAGES
```
When the retention type is set to MESSAGES, the station will only hold up to retention_value messages. The station will delete the oldest messsages to maintain a retention_value number of messages.

```c#
RetentionTypes.BYTES
```
When the retention type is set to BYTES, the station will only hold up to retention_value BYTES. The oldest messages will be deleted in order to maintain at maximum retention_vlaue BYTES in the station.

```c#
RetentionTypes.ACK_BASED
```
When the retention type is set to ACK_BASED, messages in the station will be deleted after they are acked by all subscribed consumer groups.

### Retention Values

The unit of the `retention_value` changes depending on the `retention_type` specified. 

All retention values are of type `int`. The following units are used based on the respective retention type:

`RetentionTypes.MAX_MESSAGE_AGE_SECONDS` is represented **in seconds**,<br>
`RetentionTypes.MESSAGES` is a **number of messages**,<br>
`RetentionTypes.BYTES` is a **number of bytes**,<br>
With `RetentionTypes.ACK_BASED` the `RetentionType` is ignored. 

### Storage Types

Memphis currently supports the following types of messages storage:

```c#
StorageTypes.DISK
```
When storage is set to DISK, messages are stored on disk.

```c#
StorageTypes.MEMORY
```
When storage is set to MEMORY, messages are stored in the system memory.

### Destroying a Station

Destroying a station will remove all its resources (including producers and consumers).

```c#
station.DestroyAsync()
```

### Creating a new Schema
In case schema is already exist a new version will be created

```c#
await client.CreateSchema("<schema-name>", "<schema-type>", "<schema-file-path>")
```

### Enforcing a Schema on an Existing Station

```c#
await client.EnforceSchema(stationName: "<station-name>", schemaName: "<schema-name>");
```

### Deprecated - Attaching Schema

The `AttachSchema` method is depricated, use `EnforceSchema` instead.
```c#
await client.AttachSchema(stationName: "<station-name>", schemaName: "<schema-name>");
```

### Detaching a Schema from Station
```c#
await client.DetachSchema(stationName: station.Name);
```


### Produce and Consume messages

The most common client operations are using `produce` to send messages and `consume` to
receive messages.

Messages are published to a station with a Producer and consumed from it by a Consumer. 

Consumers are poll based and consume all the messages in a station. Consumers can also be grouped into consumer groups. When consuming with a consumer group, all consumers in the group will receive each message.

Memphis messages are payload agnostic. Payloads are always `byte[]`s.

In order to stop getting messages, you have to call `consumer.Dispose()`. Destroy will terminate the consumer even if messages are currently being sent to the consumer.

If a station is created with more than one partition, producing to and consuming from the station will happen in a round robin fashion. 

### Creating a Producer

```c#
try
{
   // First: creating Memphis client
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.Username = "<username>";
    options.Password = "<password>";
    var client = await MemphisClientFactory.CreateClient(options);

    // Second: creating the Memphis producer 
    var producer = await client.CreateProducer(new MemphisProducerOptions
    {
        StationName = "<memphis-station-name>",
        ProducerName = "<memphis-producer-name>"
    });
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

### Producing a message

```c#
var commonHeaders = new NameValueCollection();
commonHeaders.Add("key-1", "value-1");

await producer.ProduceAsync(
    message: Encoding.UTF8.GetBytes(text),
    headers:commonHeaders
);
```
Note:
When producing to a station with more than one partition, the producer will produce messages in a Round Robin fashion between the different partitions.

The ProduceAsync method allows for the user to produce a message without discretely creating a producer. Because this creates a producer for every message, it is better to create a producer if many message need to be produced. 

For message data formats see [here](https://docs.memphis.dev/memphis/memphis-schemaverse/formats/produce-consume). 

Messages produced by ProduceAsync run asyncronously by default. By using the AsyncProduce Option this can be set to produce messages syncronously, waiting for an ack after each message is produced. By default, messages are sent while still waiting for the ack of previously sent messages. This reduces preceived network latency and will allow for producers to produce more messages however may incur a loss in reliability. 

Here is an example of a ProduceAsync method call that waits up to one minute for an acknowledgement from memphis and produces messages syncronously:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    await memphisClient.ProduceAsync(
        options: new MemphisProducerOptions
        {
            MaxAckTimeMs = 60_000,
            StationName = "MyStation",
            ProducerName = "MyProducer"
        },
        message: Encoding.UTF8.GetBytes("MyMessage"),
        asyncProduceAck: false
    );
}
catch (Exception ex)
{
    // handle exception
}
```

As discussed before in the station section, idempotency is an important feature of memphis. To achieve idempotency, an id must be assigned to messages that are being produced. Use the messageId parameter for this purpose.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    await memphisClient.ProduceAsync(
        options: new MemphisProducerOptions
        {
            MaxAckTimeMs = 60_000,
            StationName = "MyStation",
            ProducerName = "MyProducer"
        },    
        message: Encoding.UTF8.GetBytes("MyMessage"),
        messageId: "UniqueMessageID"
    );
}
catch (Exception ex)
{
    // handle exception
}
```

To add message headers to the message, use the headers parameter. Headers can help with observability when using certain 3rd party to help monitor the behavior of memphis. See [here](https://docs.memphis.dev/memphis/memphis-broker/comparisons/aws-sqs-vs-memphis#observability) for more details.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var headers = new NameValueCollection
    {
        { "trace_header", "track_me_123" }
    };

    await memphisClient.ProduceAsync(
    options: new MemphisProducerOptions
    {
        MaxAckTimeMs = 60_000,
        StationName = "MyStation",
        ProducerName = "MyProducer"
    },
    message: Encoding.UTF8.GetBytes("MyMessage"),
    headers: headers
    );
}
catch (Exception ex)
{
    // handle exception
}
```

Memphis can produce to a specific partition in a station. To do so, use the partitionKey parameter:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    await memphisClient.ProduceAsync(
        options: new MemphisProducerOptions
        {
            MaxAckTimeMs = 60_000,
            StationName = "MyStation",
            ProducerName = "MyProducer"
        },    
        message: Encoding.UTF8.GetBytes("MyMessage")
        partitionKey: "Partition3"
    );
}
catch (Exception ex)
{
    // handle exception
}
```

### Produce using partition number
The partition number will be used to produce messages to a spacific partition.

```csharp
await producer.ProduceAsync(
    message: Encoding.UTF8.GetBytes(text),
    headers:commonHeaders, 
    partitionNumber:<int> // default is -1
);
```

### Produce to multiple stations

To produce a message to multiple stations, use the `StationNames` property of the `MemphisProducerOptions`:

```csharp
try
{
    // First: creating Memphis client
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.Username = "<username>";
    options.Password = "<password>";
    var client = await MemphisClientFactory.CreateClient(options);

    // Second: creating the Memphis producer 
    var producer = await client.CreateProducer(new MemphisProducerOptions
    {
        StationName = "<memphis-station-name>",
        ProducerName = "<memphis-producer-name>",
        StationNames = new string[] { "<memphis-station-name-1>", "<memphis-station-name-2>" }
    });

    // Third: sending the message
    var headers = new NameValueCollection
    {
        { "key-1", "value-1" }
    };
    await producer.ProduceAsync(
        message: Encoding.UTF8.GetBytes("Hello World!"),
        headers:headers
    );
}
catch (Exception ex)
{
    // handle exception
}
```

It is also possible to produce a message to multiple stations using the `ProduceAsync` method of the `MemphisClient`:

```csharp
// First: creating Memphis client
var options = MemphisClientFactory.GetDefaultOptions();
options.Host = "<memphis-host>";
options.Username = "<username>";
options.Password = "<password>";

var client = await MemphisClientFactory.CreateClient(options);

var headers = new NameValueCollection
{
    { "trace_header", "track_me_123" }
};

// Second: sending the message
await client.ProduceAsync(
    options: new MemphisProducerOptions
    {
        MaxAckTimeMs = 60_000,
        ProducerName = "MyProducer",
        StationNames = new string[] { "<memphis-station-name-1>", "<memphis-station-name-2>" }
    },
    message: Encoding.UTF8.GetBytes("MyMessage"),
    headers: headers
);
```

### Destroying a Producer

```c#
await producer.DestroyAsync()
```

### Creating a Consumer

```c#
try
{
    // First: creating Memphis client
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    options.Username = "<username>";
    options.Password = "<password>";
    var client = await MemphisClientFactory.CreateClient(options);
    
    // Second: creaing Memphis consumer
    var consumer = await client.CreateConsumer(new ConsumerOptions
    {
        StationName = "<station-name>",
        ConsumerName = "<consumer-name>",
        ConsumerGroup = "<consumer-group-name>",
    }); 
       
}
catch (Exception ex)
{
    Console.Error.WriteLine("Exception: " + ex.Message);
    Console.Error.WriteLine(ex);
}
```

Note:
When consuming from a station with more than one partition, the consumer will consume messages in Round Robin fashion from the different partitions.

Use the Memphis CreateConsumer method to create a Consumer. Consumers are used to pull messages from stations.

Here is an example on how to create a consumer with all of the default options:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer"
    });
}
catch (Exception ex)
{
    // handle exception
}handle exception
}
```

To create a consumer in a consumer group, add the ConsumerGroup MemphisConsumerOptions:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        ConsumerGroup = "MyConsumerGroup1"
    });
}
catch (Exception ex)
{
    // handle exception
}
```

When using Consumer.consume, the consumer will continue to consume in an infinite loop. To change the rate at which the consumer polls, change the PullIntervalMs parameter:

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        PullIntervalMs = 2_000
    });
}
catch (Exception ex)
{
    // handle exception
}
```

Every time the consumer polls, the consumer will try to take BatchSize number of elements from the station. However, sometimes there are not enough messages in the station for the consumer to consume a full batch. In this case, the consumer will continue to wait until either BatchSize messages are gathered or the time in milliseconds specified by BatchMaxTimeToWaitMs is reached. 

Here is an example of a consumer that will try to pull 100 messages from a station every 10 seconds while waiting up to 15 seconds for all messages to reach the consumer.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        PullIntervalMs = 10_000,
        BatchSize = 100,
        BatchMaxTimeToWaitMs = 1000
    });
}
catch (Exception ex)
{
    // handle exception
}
```

The MaxMsgDeliveries parameter allows the user how many messages the consumer is able to consume before consuming more.

```csharp
try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "localhost";
    options.Username = "root";
    options.Password = "memphis";
    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(new MemphisConsumerOptions
    {
        StationName = "MyStation",
        ConsumerName = "MyConsumer",
        PullIntervalMs = 10_000,
        BatchSize = 100,
        BatchMaxTimeToWaitMs = 1000,
        MaxMsgDeliveries = 2
    });
}
catch (Exception ex)
{
    // handle exception
}
```

### Creating message handler for consuming a message

To configure message handler, use the `MessageReceived` event:

```c#
consumer.MessageReceived += (sender, args) =>
{
    if (args.Exception != null)
    {
        Console.Error.WriteLine(args.Exception);
        return;
    }

    foreach (var msg in args.MessageList)
    {
        //print message itself
        Console.WriteLine("Received data: " + Encoding.UTF8.GetString(msg.GetData()));


        // print message headers
        foreach (var headerKey in msg.GetHeaders().Keys)
        {
            Console.WriteLine(
                $"Header Key: {headerKey}, value: {msg.GetHeaders()[headerKey.ToString()]}");
        }

        Console.WriteLine("---------");
        msg.Ack();
    }
    Console.WriteLine("destroyed");
};
```

### Consuming a message

The consumer will try to fetch messages every _PullIntervalMs_ (that was given in Consumer's creation) and call the defined message handler.

```c#
 await consumer.ConsumeAsync();
```


#### Consumer schema deserialization
To get messages deserialized, use `msg.GetDeserializedData()` or  `msg.GetDeserializedData<T>()`.  

```csharp
consumer.MessageReceived += (sender, args) =>
{
    if (args.Exception != null)
    {
        Console.Error.WriteLine(args.Exception);
        return;
    }

    foreach (var msg in args.MessageList)
    {
        Console.WriteLine($"Received data: {msg.GetDeserializedData()}");
        msg.Ack();
    }
};
```

There may be some instances where you apply a schema *after* a station has received some messages. In order to consume those messages get_data_deserialized may be used to consume the messages without trying to apply the schema to them. As an example, if you produced a string to a station and then attached a protobuf schema, using get_data_deserialized will not try to deserialize the string as a protobuf-formatted message.

### Fetch a single batch of messages

Using fetch_messages or fetch will allow the user to remove a specific number of messages from a given station. This behavior could be beneficial if the user does not want to have a consumer actively poll from a station indefinetly.

```c#
client.FetchMessages(new FetchMessageOptions
{
    StationName= "<station-name>",
    ConsumerName= "<consumer-name>",
    ConsumerGroup= "<group-name>", // defaults to the consumer name.
    BatchSize= 10, // defaults to 10
    BatchMaxTimeToWaitMs= 1000, // defaults to 1000
    MaxAckTimeMs= 30000, // defaults to 30000
    MaxMsgDeliveries= 2, // defaults to 2
    StartConsumeFromSequence= 1, // start consuming from a specific sequence. defaults to 1
    LastMessages= -1 // consume the last N messages, defaults to -1 (all messages in the station)
});
```

### Fetch a single batch of messages after creating a consumer

`prefetch = true` will prefetch next batch of messages and save it in memory for future Fetch() request \
Note: Use a higher MaxAckTime as the messages will sit in a local cache for some time before processing

```C#
var messages = consumer.Fetch(
    batchSize: 10,
    prefetch: true
);
```

### Acknowledging a Message

Acknowledging a message indicates to the Memphis server to not re-send the same message again to the same consumer or consumers group.

```c#
msg.Ack();
```

### Nacking a Message

Mark the message as not acknowledged - the broker will resend the message immediately to the same consumers group, instead of waiting to the max ack time configured.

```C#
msg.Nack();
```

### Sending a message to the dead-letter

Sending the message to the dead-letter station (DLS) - the broker won't resend the message again to the same consumers group and will place the message inside the dead-letter station (DLS) with the given reason.
The message will still be available to other consumer groups

```shell
msg.DeadLetter("reason");
```

### Delay the message after a given duration

Delay the message and tell Memphis server to re-send the same message again to the same consumer group.\
The message will be redelivered only in case `Consumer.MaxMsgDeliveries` is not reached yet.

```C#
msg.Delay(<delayMilliSeconds>);
```

### Get headers

Get headers per message

```c#
msg.GetHeaders()
```

### Get message sequence number 

Get message sequence number

```C#
var sequence = msg.GetSequence();
```

### Get message time sent 

Get message time sent

```C#
var dateTime = msg.GetTimeSent();
```

### Destroying a Consumer

```c#
await consumer.DestroyAsync();
```

### Check if broker is connected

```c#
memphisClient.IsConnected();
```

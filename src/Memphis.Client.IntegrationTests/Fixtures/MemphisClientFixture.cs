using System.Collections.Specialized;
using Memphis.Client.Station;

namespace Memphis.Client.IntegrationTests.Fixtures;

public class MemphisClientFixture
{
    internal readonly ClientOptions MemphisClientOptions;
    internal readonly StationOptions DefaultStationOptions;
    internal NameValueCollection CommonHeaders;

    public MemphisClientFixture()
    {
        MemphisClientOptions = MemphisClientFactory.GetDefaultOptions();
        MemphisClientOptions.Username = "root";
        MemphisClientOptions.Host = "localhost";
        MemphisClientOptions.Password = "memphis";
        DefaultStationOptions = new StationOptions
        {
            Name = "default",
            RetentionType = RetentionTypes.MAX_MESSAGE_AGE_SECONDS,
            RetentionValue = 86_400,
            StorageType = StorageTypes.DISK,
            Replicas = 1,
            IdempotenceWindowMs = 0,
            SendPoisonMessageToDls = true,
            SendSchemaFailedMessageToDls = true,
            PartitionsNumber = 3
        };

        CommonHeaders = new NameValueCollection();
        CommonHeaders.Add("key-1", "value-1");
    }

    internal async Task<MemphisStation> SetupStationAsync(MemphisClient client, string stationName)
    {
        var options = DefaultStationOptions;
        options.Name = stationName;

        return await client.CreateStation(options);
    }
}
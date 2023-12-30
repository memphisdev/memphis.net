using System.Data;
using Memphis.Client.Producer;
using Memphis.Client.Station;

namespace Memphis.Client.IntegrationTests.Producer;

[Collection(CollectionFixtures.MemphisClient)]
public class MemphisProducerTests
{
    private readonly MemphisClientFixture _fixture;
    public MemphisProducerTests(MemphisClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("producer_tst_station_a", "producer_tst_producer_a", true)]
    public async Task GivenProducerOptions_WhenCreateProducer_ThenProducerIsCreated(
        string stationName, string producerName, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var station = await _fixture.SetupStationAsync(client, stationName);
        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };

        var producer = await client.CreateProducer(producerOptions);

        await producer.DestroyAsync();
        await station.DestroyAsync();

        Assert.NotNull(station);
        Assert.NotNull(producer);
    }

    [Theory]
    [InlineData("producer_tst_station_b", "producer_tst_producer_b", true, "Hello, World!")]
    public async Task GivenProducerOptions_WhenProduceAsync_ThenMessageIsProduced(
        string stationName, string producerName, bool generateUniqueSuffix, string message)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var station = await _fixture.SetupStationAsync(client, stationName);

        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var producer = await client.CreateProducer(producerOptions);

        await producer.ProduceAsync(message, _fixture.CommonHeaders);

        await producer.DestroyAsync();
        await station.DestroyAsync();

        Assert.NotNull(station);
        Assert.NotNull(producer);
    }

    [Theory]
    [InlineData("producer_tst_station_destroy_c", "producer_tst_producer_destroy_c", true)]
    public async Task GivenProducerOptions_WhenDestroyAsync_ThenProducerIsDestroyed(
        string stationName, string producerName, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var station = await _fixture.SetupStationAsync(client, stationName);

        var producerOptions = new MemphisProducerOptions
        {
            StationName = stationName,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix
        };
        var producer = await client.CreateProducer(producerOptions);

        await producer.DestroyAsync();
        await station.DestroyAsync();

        Assert.NotNull(station);
        Assert.NotNull(producer);
    }

    [Theory]
    [InlineData(
        "broadcast_tst_producer", 
        true,
        "broadcast_station_1", "broadcast_station_2", "broadcast_station_3"
    )]
    public async Task GivenMultiStationProducer_WhenProduce_ThenProduceToAllStations(
        string producerName, 
        bool generateUniqueSuffix, 
        params string[] stationNames)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        var producer = await client.CreateProducer(new MemphisProducerOptions
        {
            StationNames = stationNames,
            ProducerName = producerName,
            GenerateUniqueSuffix = generateUniqueSuffix,
        });

        await producer.ProduceAsync("Broadcast Message", _fixture.CommonHeaders);

        await producer.DestroyAsync();
    }


    [Theory]
    [InlineData("loop_tst_producer_1","loop_producer_station_1", true)]
    [InlineData("loop_tst_producer_2","loop_producer_station_2", false)]
    public async Task GivenProducerOptions_WhenRecreateProducerMultipleTimes_ThenProducerIsRecreated(
        string producerName, string stationName, bool generateUniqueSuffix)
    {
        using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);
        bool noError = true;
        
        for (int i = 0; i < 10; i++)
        {
            var producerOptions = new MemphisProducerOptions
            {
                StationName = stationName,
                ProducerName = producerName,
                GenerateUniqueSuffix = generateUniqueSuffix
            };
            var producer = await client.CreateProducer(producerOptions);
            await producer.DestroyAsync();
        }

        Assert.True(noError);
    }


    // [Theory]
    // [InlineData("infinite_st", "infinite_produce", true)]
    // public async Task ProduceInfinitely(
    //     string stationName, string producerName, bool generateUniqueSuffix)
    // {
    //     using var client = await MemphisClientFactory.CreateClient(_fixture.MemphisClientOptions);

    //     var producerOptions = new MemphisProducerOptions
    //     {
    //         StationName = stationName,
    //         ProducerName = producerName,
    //         GenerateUniqueSuffix = generateUniqueSuffix
    //     };
    //     var producer = await client.CreateProducer(producerOptions);

    //     int counter = 0;
    //     while (true)
    //     {
    //         string message = $"Hello, World! {counter}";
    //         await producer.ProduceAsync(message, _fixture.CommonHeaders);
    //         Console.WriteLine(message);
    //         await Task.Delay(TimeSpan.FromSeconds(1));
    //         counter+=1;
    //     }

    //     await producer.DestroyAsync();

    //     Assert.NotNull(producer);
    // }
}
using Memphis.Client;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;

MemphisClient? memphisClient = null;

try
{
    var options = MemphisClientFactory.GetDefaultOptions();
  
    options.Host = "<memphis-host>";
    // options.AccountId = <memphis-accountId>;
    options.Username = "<memphis-username>";
    options.Password = "<memphis-password>";

    memphisClient = await MemphisClientFactory.CreateClient(options);

    var producer = await memphisClient.CreateProducer(
        new Memphis.Client.Producer.MemphisProducerOptions
        {
            StationName = "<station-name>",
            ProducerName = "<producer-name>"
        });

    Message message = new()
    {
        Hello = "World!"
    };

    var headers = new NameValueCollection();

    for (int i = 0; i < 3; i++)
    {
        var msgBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await producer.ProduceAsync(
          msgBytes,
          headers);
    }

    memphisClient.Dispose();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    memphisClient?.Dispose();
}

public class Message
{
    public string? Hello { get; set; }
}
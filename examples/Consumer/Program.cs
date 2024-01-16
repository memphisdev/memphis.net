using Memphis.Client;
using Memphis.Client.Core;
using System.Text.Json;

MemphisClient? memphisClient = null;

try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "<memphis-host>";
    // options.AccountId = "<memphis-accountId>";
    options.Username = "<memphis-username>";
    options.Password = "<memphis-password>";

    memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(
       new Memphis.Client.Consumer.MemphisConsumerOptions
       {
           StationName = "<station-name>",
           ConsumerName = "<consumer-name>"
       });

    while (true) {
        var messages = consumer.Fetch(3, false);

        if (!messages.Any())
        {
            continue;
        }

        foreach (MemphisMessage message in messages)
        {
            var messageData = message.GetData();
            var messageOBJ = JsonSerializer.Deserialize<Message>(messageData);

            // Do something with the message here
            Console.WriteLine(JsonSerializer.Serialize(messageOBJ));

            message.Ack();
        }
    }
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
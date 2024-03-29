﻿using Memphis.Client;
using Memphis.Client.Core;
using System.Text.Json;

try
{
    var options = MemphisClientFactory.GetDefaultOptions();
    options.Host = "aws-us-east-1.cloud.memphis.dev";
    options.AccountId = int.Parse(Environment.GetEnvironmentVariable("memphis_account_id"));
    options.Username = "test_user";
    options.Password = Environment.GetEnvironmentVariable("memphis_pass");

    var memphisClient = await MemphisClientFactory.CreateClient(options);

    var consumer = await memphisClient.CreateConsumer(
       new Memphis.Client.Consumer.MemphisConsumerOptions
       {
           StationName = "test_station",
           ConsumerName = "consumer"
       });

    var messages = consumer.Fetch(3, false);

    foreach (MemphisMessage message in messages)
    {
        var messageData = message.GetData();
        var messageOBJ = JsonSerializer.Deserialize<Message>(messageData);

        // Do something with the message here
        Console.WriteLine(JsonSerializer.Serialize(messageOBJ));

        message.Ack();
    }

    memphisClient.Dispose();

}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

public class Message
{
    public string Hello { get; set; }
}
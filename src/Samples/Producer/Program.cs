﻿using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Memphis.Client;
using NATS.Client.Internals;

namespace Producer
{
    class ProducerApp
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var options = MemphisClientFactory.GetDefaultOptions();
                options.Host = "localhost";
                options.Username = "dotnetapp";
                options.ConnectionToken = "memphis";
                var client = MemphisClientFactory.CreateClient(options);

                var producer = await client.CreateProducer("test-station", "dotnetapp", true);

                var commonHeaders = new NameValueCollection();
                commonHeaders.Add("key-1", "value-1");

                for (int i = 0; i < 10_000000; i++)
                {
                    await Task.Delay(1_000);
                    var text = $"Message #{i}: Welcome to Memphis";
                    await producer.ProduceAsync(Encoding.UTF8.GetBytes(text), commonHeaders);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}
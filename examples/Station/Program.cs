﻿using System;
using System.Threading.Tasks;
using Memphis.Client;
using Memphis.Client.Station;

namespace Station
{
    class StationApp
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var options = MemphisClientFactory.GetDefaultOptions();
                options.Host = "<memphis-host>";
                options.Username = "<username>";
                options.Password = "<password>";
                // options.AccountId = <account-id>;
                // The AccountId field should be sent only on the cloud version of Memphis, otherwise it will be ignored.
                var client = await MemphisClientFactory.CreateClient(options);

                var station = await client.CreateStation(
                    stationOptions: new StationOptions()
                    {
                        Name = "<station-name>",
                        RetentionType = RetentionTypes.MAX_MESSAGE_AGE_SECONDS,
                        RetentionValue = 3600,
                        StorageType = StorageTypes.DISK,
                        Replicas = 1,
                        IdempotenceWindowMs = 0,
                        SendPoisonMessageToDls = true,
                        SendSchemaFailedMessageToDls = true,
                    });

                Console.WriteLine("Station created successfully...");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: " + ex.Message);
                Console.Error.WriteLine(ex);
            }
        }
    }
}
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

public static class Server_Simple_Samples
{
    private static MqttServer mqttServer;
    private static List<string> storedInitialConnectionMessages = new List<string>();

    public static async Task Run_Server_With_Logging()
    {
        var mqttFactory = new MqttFactory();

        var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();

        mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

        // Output all received messages to the console window.
        mqttServer.ApplicationMessageReceived += (sender, eventArgs) =>
        {
            Console.WriteLine($"Received message on topic: {eventArgs.ApplicationMessage.Topic}, Payload: {Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)}");

            // Process "initial_connection" topic separately
            if (eventArgs.ApplicationMessage.Topic == "initial_connection")
            {
                ProcessInitialConnectionData(eventArgs.ApplicationMessage);
            }
        };

        await mqttServer.StartAsync();

        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();

        await mqttServer.StopAsync();
    }

    private static void ProcessInitialConnectionData(MqttApplicationMessage message)
    {
        // Get the payload as a string
        string payload = Encoding.UTF8.GetString(message.Payload);

        // Store the message in the storage if the payload is not "RequestingTopicInfo"
        if (payload != "RequestingTopicInfo")
        {
            storedInitialConnectionMessages.Add(payload);
            Console.WriteLine($"Stored message from 'initial_connection': {payload}");
        }
        else
        {
            // If the payload is "RequestingTopicInfo", publish the stored messages to 'request_topic_info'
            if (storedInitialConnectionMessages.Count > 0)
            {
                Console.WriteLine($"Publishing stored messages to 'request_topic_info'");
                foreach (var storedMessage in storedInitialConnectionMessages)
                {
                    var responseMessage = new MqttApplicationMessageBuilder()
                        .WithTopic("request_topic_info")
                        .WithPayload(storedMessage)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithRetainFlag(false)
                        .Build();

                    mqttServer.PublishAsync(responseMessage);
                }

                // Clear the stored messages after publishing
                storedInitialConnectionMessages.Clear();
            }
            else
            {
                Console.WriteLine($"No stored messages to publish to 'request_topic_info'");
            }
        }
    }
}

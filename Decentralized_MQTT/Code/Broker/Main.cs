using System;
using System.Threading.Tasks;
using MQTTnet.Samples.Server;

class Program
{
    static async Task Main(string[] args)
    {
        await Server_Simple_Samples.Run_Server_With_Logging();
    }
}

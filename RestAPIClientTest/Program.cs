using ModelLibrary.Data;
using System;

namespace RestAPIClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new NetMQClient();

            client.GetSmallPayloadAsync().GetAwaiter().GetResult();
            client.GetLargePayloadAsync().GetAwaiter().GetResult();
            client.PostLargePayloadAsync(MeteoriteLandingData.GrpcMeteoriteLandingList).GetAwaiter().GetResult();
            client.GetLargePayloadAsyncMultipart().GetAwaiter().GetResult();
            client.PostLargePayloadAsyncMultipart(MeteoriteLandingData.GrpcMeteoriteLandings).GetAwaiter().GetResult();

            client.Dispose();

            Console.WriteLine("Hello World!");
        }
    }
}

using System;

namespace NetMQApi
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new NetMQServer();

            server.Start();

            Console.WriteLine("NetMQ MeteoriteLandingServer Running on localhost:5555");
            Console.ReadKey();

            server.Stop();
        }
    }
}

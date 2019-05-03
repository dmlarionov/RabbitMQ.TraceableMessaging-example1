using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace lib
{
    public static class Utils
    {
        public static IConnection ConnectRabbitMQ(IConfiguration config)
        {
            var factory = new ConnectionFactory()
            {
                HostName = config["RabbitMQ:Connection:Hostname"],
                UserName = config["RabbitMQ:Connection:Username"],
                Password = config["RabbitMQ:Connection:Password"],
                VirtualHost = config["RabbitMQ:Connection:Vhost"]
            };

            // make retries with following waits
            int[] wait = {500, 500, 1000, 1000, 1000, 1000, 2000, 2000, 2000, 2000, 2000};
            for (int i = 0; i < wait.Length; i++)
            {
                try
                {
                    return factory.CreateConnection();
                }
                catch(BrokerUnreachableException)
                {
                    Thread.Sleep(wait[i]);
                }
            }
            throw new Exception($"Can't connect to RabbitMQ (hostname: {config["RabbitMQ:Connection:Hostname"]})");
        }
    }
}
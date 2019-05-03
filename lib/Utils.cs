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

            // make retries after 1ms, 10ms, 0.1s, 1s, 5s, 10s
            int[] wait = {100, 500, 1000, 2500, 5000, 7500};
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
            throw new Exception("Can't connect to RabbitMQ");
        }
    }
}
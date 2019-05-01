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

            // make retries after 3ms, 30ms, 0.3s, 3s, 30s
            int wait = 3;
            bool continue_ = true;
            do
            {
                try
                {
                    return factory.CreateConnection();
                }
                catch(BrokerUnreachableException)
                {
                    if (wait <= 30000)  // less than 30s
                    {
                        Thread.Sleep(wait);
                        wait *=10;
                    }
                    else
                        continue_ = false;
                }
            }
            while(continue_);
            throw new Exception("Can't connect to RabbitMQ");
        }
    }
}
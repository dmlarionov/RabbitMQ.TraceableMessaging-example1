using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using RabbitMQ.Client;
using RabbitMQ.TraceableMessaging.ApplicationInsights;
using RabbitMQ.TraceableMessaging.ApplicationInsights.Models;
using RabbitMQ.TraceableMessaging.EventArgs;
using RabbitMQ.TraceableMessaging.Jwt.Models;
using RabbitMQ.TraceableMessaging.Options;

namespace lib
{
    public abstract class FanoutListener<TFormatOptions> : IDisposable
        where TFormatOptions : FormatOptions, new()
    {
        private IModel _channel;
        private Consumer<JwtSecurityContext> _subscriber;

        public FanoutListener(
            string exchange, 
            IConnection conn)
        {
            _channel = conn.CreateModel();
            try
            {
                _channel.ExchangeDeclare(
                    exchange: exchange,
                    type: "fanout",
                    durable: false,
                    autoDelete: false);
            }
            catch (Exception)
            {
                // don't care about interference on exchange declaration
            }
            var queue = _channel.QueueDeclare(exclusive: true);
            _channel.QueueBind(queue.QueueName, exchange, "");
            _subscriber = new Consumer<JwtSecurityContext>(
                _channel,
                new ConsumeOptions(queue) { AutoAck = true },
                new TFormatOptions());
            _subscriber.Received += OnReceive;
        }

        protected abstract void OnReceive(object sender, RequestEventArgsBase ea);

        public void Dispose()
        {
            _subscriber.Received -= OnReceive;
            _channel.Dispose();
        }
    }
}

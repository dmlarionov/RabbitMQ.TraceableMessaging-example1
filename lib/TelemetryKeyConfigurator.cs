using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using RabbitMQ.Client;
using RabbitMQ.TraceableMessaging.ApplicationInsights;
using RabbitMQ.TraceableMessaging.ApplicationInsights.Models;
using RabbitMQ.TraceableMessaging.EventArgs;
using RabbitMQ.TraceableMessaging.YamlDotNet.Options;
using RabbitMQ.TraceableMessaging.Jwt.Models;
using RabbitMQ.TraceableMessaging.Options;

namespace lib
{
    /// <summary>
    /// Service designed to set Application Insights instrumentation key 
    /// to remote peers by sending it over RabbitMQ
    /// </summary>
    public class TelemetryKeyConfigurator : IDisposable
    {
        private IModel _channel;
        private Consumer<JwtSecurityContext> _subscriber;
        private TaskCompletionSource<bool> _completion;
        
        public TelemetryKeyConfigurator(string exchange, IConnection conn, TaskCompletionSource<bool> completion)
        {
            _channel = conn.CreateModel();
            _completion = completion;
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
                new YamlFormatOptions());
            _subscriber.Received += OnReceive;
        }

        private void OnReceive(object sender, RequestEventArgsBase ea)
        {
            TelemetryConfiguration.Active.InstrumentationKey = ea.GetRequest<string>();
            _completion.SetResult(true);
        }
        
        public void Dispose()
        {
            _subscriber.Received -= OnReceive;
            _channel.Dispose();
        }

        /// <summary>
        /// Set my App Insights key by receiving it from RabbitMQ exchange
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <returns></returns>
        public static Task ConfigureAsync(string exchange, IConnection conn)
        {
            var completion = new TaskCompletionSource<bool>();
            var inst = new TelemetryKeyConfigurator(exchange, conn, completion);
            return completion.Task.ContinueWith(x => inst.Dispose());
        }

        /// <summary>
        /// Send App Insights key to peers through RabbitMQ exchange
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="key">Key value</param>
        /// <returns></returns>
        public static Task ConfigurePeersAsync(string exchange, IConnection conn, string key)
        {
            return (new Publisher(
                conn.CreateModel(), 
                new PublishOptions() { Exchange = exchange }, 
                new YamlFormatOptions())
            ).SendAsync(key);
        }
    }
}
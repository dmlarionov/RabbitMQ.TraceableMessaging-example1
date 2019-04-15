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
    /// Set Application Insights instrumentation key on remote peers by sending it over RabbitMQ
    /// </summary>
    public sealed class TelemetryKeyConfigurator : FanoutListener<YamlFormatOptions>
    {
        private TaskCompletionSource<bool> _completion;

        public TelemetryKeyConfigurator(
            string exchange,
            IConnection conn, 
            TaskCompletionSource<bool> completion) : base(
                exchange,
                conn)
        {
            _completion = completion;
        }

        protected override void OnReceive(object sender, RequestEventArgsBase ea)
        {
            TelemetryConfiguration.Active.InstrumentationKey = ea.GetRequest<string>();
            _completion.SetResult(true);
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
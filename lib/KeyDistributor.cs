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
using lib.DTO;

namespace lib
{
    /// <summary>
    /// Set keys on remote peers by sending it over RabbitMQ
    /// </summary>
    public sealed class KeyDistributor : FanoutListener<YamlFormatOptions>
    {
        private readonly TaskCompletionSource<bool> _completion;
        private readonly Action<Keys> _action;

        public KeyDistributor(
            string exchange,
            IConnection conn,
            Action<Keys> action,
            TaskCompletionSource<bool> completion) : base(
                exchange,
                conn)
        {
            _action = action;
            _completion = completion;
        }

        protected override void OnReceive(object sender, RequestEventArgsBase ea)
        {
            _action(ea.GetRequest<Keys>());
            _completion.SetResult(true);
        }

        /// <summary>
        /// Set my keys using action by receiving key form RabbitMQ exchange
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="action">Action to set key</param>
        /// <returns></returns>
        public static Task ConfigureAsync(string exchange, IConnection conn, Action<Keys> action)
        {
            var completion = new TaskCompletionSource<bool>();
            var inst = new KeyDistributor(exchange, conn, action, completion);
            return completion.Task.ContinueWith(x => inst.Dispose());
        }

        /// <summary>
        /// Set peer keys by sending it through RabbitMQ exchange
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="keys">Key object</param>
        /// <returns></returns>
        public static Task ConfigurePeersAsync(string exchange, IConnection conn, Keys keys)
        {
            return (new Publisher(
                conn.CreateModel(),
                new PublishOptions() { Exchange = exchange },
                new YamlFormatOptions())
            ).SendAsync(keys);
        }
    }
}

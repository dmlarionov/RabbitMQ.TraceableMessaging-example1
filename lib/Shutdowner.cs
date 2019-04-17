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
    /// Distribute command to shutdown
    /// </summary>
    public sealed class Shutdowner : FanoutListener<YamlFormatOptions>
    {
        private readonly TaskCompletionSource<bool> _completion;
        private readonly Action _action;

        public Shutdowner(
            string exchange,
            IConnection conn,
            Action action,
            TaskCompletionSource<bool> completion) : base(
                exchange,
                conn)
        {
            _action = action;
            _completion = completion;
        }

        protected override void OnReceive(object sender, RequestEventArgsBase ea)
        {
            _action();
            _completion.SetResult(true);
        }

        /// <summary>
        /// Wait for shutdown command then execute action
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="action">Action to set key</param>
        /// <returns></returns>
        public static Task ShutdownAsync(string exchange, IConnection conn, Action action)
        {
            var completion = new TaskCompletionSource<bool>();
            var inst = new Shutdowner(exchange, conn, action, completion);
            return completion.Task.ContinueWith(x => inst.Dispose());
        }

        /// <summary>
        /// Shutdown peers
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <returns></returns>
        public static Task ShutdownPeersAsync(string exchange, IConnection conn)
        {
            return (new Publisher(
                conn.CreateModel(),
                new PublishOptions() { Exchange = exchange },
                new YamlFormatOptions())
            ).SendAsync(new object());
        }
    }
}

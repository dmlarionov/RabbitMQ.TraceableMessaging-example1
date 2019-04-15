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
    /// Set JWT issuer key on remote peers by sending it over RabbitMQ
    /// </summary>
    public sealed class TokenIssuerKeyDistributor : FanoutListener<YamlFormatOptions>
    {
        private TaskCompletionSource<bool> _completion;
        private Action<byte[]> _action;

        public TokenIssuerKeyDistributor(
            string exchange,
            IConnection conn,
            Action<byte[]> action,
            TaskCompletionSource<bool> completion) : base(
                exchange,
                conn)
        {
            _action = action;
            _completion = completion;
        }

        protected override void OnReceive(object sender, RequestEventArgsBase ea)
        {
            _action(ea.GetRequest<byte[]>());
            _completion.SetResult(true);
        }

        /// <summary>
        /// Set my token issuer key using action by receiving key form RabbitMQ exchange
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="action">Action to set key</param>
        /// <returns></returns>
        public static Task ConfigureAsync(string exchange, IConnection conn, Action<byte[]> action)
        {
            var completion = new TaskCompletionSource<bool>();
            var inst = new TokenIssuerKeyDistributor(exchange, conn, action, completion);
            return completion.Task.ContinueWith(x => inst.Dispose());
        }

        /// <summary>
        /// Set token issuer key to peers through RabbitMQ exchange
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="key">Key value</param>
        /// <returns></returns>
        public static Task ConfigurePeersAsync(string exchange, IConnection conn, byte[] key)
        {
            return (new Publisher(
                conn.CreateModel(),
                new PublishOptions() { Exchange = exchange },
                new YamlFormatOptions())
            ).SendAsync(key);
        }
    }
}

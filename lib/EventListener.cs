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
    public sealed class EventListener : FanoutListener<YamlFormatOptions>
    {
        private TaskCompletionSource<bool> _completion;
        private Action<byte[]> _action;

        public EventListener(
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

        public static Task ConfigureAsync(string exchange, IConnection conn, Action<byte[]> action)
        {
            var completion = new TaskCompletionSource<bool>();
            var inst = new TokenIssuerKeyDistributor(exchange, conn, action, completion);
            return completion.Task.ContinueWith(x => inst.Dispose());
        }
    }
}

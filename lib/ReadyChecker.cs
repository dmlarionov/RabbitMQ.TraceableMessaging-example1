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
using System.Collections.Generic;

namespace lib
{
    /// <summary>
    /// Check that peers are ready for demo
    /// </summary>
    public sealed class ReadyChecker : FanoutListener<YamlFormatOptions>
    {
        private IDictionary<string, bool> _ready = new Dictionary<string, bool>();
        private TaskCompletionSource<bool> _completion;

        public ReadyChecker(
            string exchange,
            IConnection conn,
            ICollection<string> peerNames,
            TaskCompletionSource<bool> completion) : base(
                exchange,
                conn)
        {
            foreach (var p in peerNames)
                _ready.Add(p, false);
            _completion = completion;
        }

        protected override void OnReceive(object sender, RequestEventArgsBase ea)
        {
            var key = ea.GetRequest<string>();
            if(_ready.ContainsKey(key)) _ready[key] = true;
            if(!_ready.Values.Contains(false)) _completion.SetResult(true);
        }

        public IList<string> NotReady()
        {
            var list = new List<string>();
            foreach (var p in _ready)
                if (!p.Value) list.Add(p.Key);
            return list;
        }

        /// <summary>
        /// Wait till every peer is ready
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="peerNames">Names of the peers</param>
        /// <returns></returns>
        public static Task WaitPeersAsync(string exchange, IConnection conn, ICollection<string> peerNames)
        {
            var completion = new TaskCompletionSource<bool>();
            var inst = new ReadyChecker(exchange, conn, peerNames, completion);
            return completion.Task.ContinueWith(x => inst.Dispose());
        }

        /// <summary>
        /// Notify that I am ready
        /// </summary>
        /// <param name="exchange">RabbitMQ exchange name</param>
        /// <param name="conn">RabbitMQ connection</param>
        /// <param name="name">My peer name</param>
        /// <returns></returns>
        public static Task SendReadyAsync(string exchange, IConnection conn, string name)
        {
            return (new Publisher(
                conn.CreateModel(),
                new PublishOptions() { Exchange = exchange },
                new YamlFormatOptions())
            ).SendAsync(name);
        }
    }
}
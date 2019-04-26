using lib.DTO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.TraceableMessaging.ApplicationInsights;
using RabbitMQ.TraceableMessaging.Json.Options;
using RabbitMQ.TraceableMessaging.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace apigw.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class DemoController : ControllerBase
    {
        private readonly RpcClient _fooClient;
        private readonly RpcClient _barClient;
        private readonly RpcClient _fibClient;

        public DemoController(IConnection conn, IConfiguration config)
        {
            var fooChannel = conn.CreateModel();
            var fooQueue = $"foo-apigw-reply-{Guid.NewGuid().ToString()}";
            fooChannel.QueueDeclare(fooQueue);
            _fooClient = new RpcClient(
                fooChannel,
                new PublishOptions(config["RabbitMQ:Services:Foo"]),
                new ConsumeOptions(fooQueue),
                new JsonFormatOptions());

            var barChannel = conn.CreateModel();
            var barQueue = $"bar-apigw-reply-{Guid.NewGuid().ToString()}";
            barChannel.QueueDeclare(barQueue);
            _barClient = new RpcClient(
                barChannel,
                new PublishOptions(config["RabbitMQ:Services:Bar"]),
                new ConsumeOptions(barQueue),
                new JsonFormatOptions());

            var fibChannel = conn.CreateModel();
            var fibQueue = $"fib-apigw-reply-{Guid.NewGuid().ToString()}";
            fibChannel.QueueDeclare(fibQueue);
            _fibClient = new RpcClient(
                fibChannel,
                new PublishOptions(config["RabbitMQ:Services:Fib"]),
                new ConsumeOptions(fibQueue),
                new JsonFormatOptions());
        }

        [HttpGet]
        public async Task<IActionResult> FooPing1() =>
            Ok(await _fooClient.GetReplyAsync<Pong1>(new Ping1(),
                HttpContext.GetTokenAsync("access_token").Result));

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BarPing2() =>
            Ok(await _barClient.GetReplyAsync<Pong2>(new Ping2(), HttpContext.GetTokenAsync("access_token").Result));

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FibPing3() =>
            Ok(await _fibClient.GetReplyAsync<Pong3>(new Ping3(), HttpContext.GetTokenAsync("access_token").Result));

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FibPing4() =>
            Ok(await _fibClient.GetReplyAsync<Pong4>(new Ping4(), HttpContext.GetTokenAsync("access_token").Result));
    }
}

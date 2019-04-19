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
        private readonly RpcClient fooClient;
        private readonly RpcClient barClient;

        public DemoController(IConnection conn, IConfiguration config)
        {
            var fooChannel = conn.CreateModel();
            var fooQueue = $"foo-apigw-reply-{Guid.NewGuid().ToString()}";
            fooChannel.QueueDeclare(fooQueue);
            fooClient = new RpcClient(
                fooChannel,
                new PublishOptions(config["RabbitMQ:Services:Foo"]),
                new ConsumeOptions(fooQueue),
                new JsonFormatOptions());

            var barChannel = conn.CreateModel();
            var barQueue = $"bar-apigw-reply-{Guid.NewGuid().ToString()}";
            barChannel.QueueDeclare(barQueue);
            barClient = new RpcClient(
                barChannel,
                new PublishOptions(config["RabbitMQ:Services:Bar"]),
                new ConsumeOptions(barQueue),
                new JsonFormatOptions());
        }

        [HttpGet]
        public async Task<IActionResult> FooPing1() =>
            Ok(await fooClient.GetReplyAsync<Pong1>(new Ping1()));

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FooPing2() =>
            Ok(await fooClient.GetReplyAsync<Pong2>(new Ping2(), HttpContext.GetTokenAsync("access_token").Result));

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BarPing1() =>
            Ok(await barClient.GetReplyAsync<Pong1>(new Ping1(), HttpContext.GetTokenAsync("access_token").Result));

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BarPing2() =>
            Ok(await barClient.GetReplyAsync<Pong2>(new Ping2(), HttpContext.GetTokenAsync("access_token").Result));
    }
}

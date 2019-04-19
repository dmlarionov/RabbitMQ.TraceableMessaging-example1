﻿using lib;
using lib.DTO;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.TraceableMessaging.ApplicationInsights.Models;
using RabbitMQ.TraceableMessaging.EventArgs;
using RabbitMQ.TraceableMessaging.Jwt.Models;
using RabbitMQ.TraceableMessaging.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace fib
{
    public sealed class FibService : Service
    {
        public FibService(
            IConnection conn,
            IConfiguration config,
            SecurityOptions securityOptions)
                : base(conn, config, securityOptions)
        {
        }

        override protected void OnReceive(object sender, RequestEventArgs<TelemetryContext, JwtSecurityContext> ea)
        {
            Console.WriteLine($"{ea.RequestType} received at {DateTime.Now}");
            switch (ea.RequestType)
            {
                case nameof(Ping1):
                    Server.Reply(ea.CorrelationId, new Pong1());
                    break;

                case nameof(Ping2):
                    Server.Reply(ea.CorrelationId, new Pong2());
                    break;

                default:
                    Server.Reply(ea.CorrelationId, new Reply { Status = ReplyStatus.Fail });
                    break;
            }
        }
    }
}
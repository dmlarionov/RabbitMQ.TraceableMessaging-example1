using RabbitMQ.TraceableMessaging.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.DTO
{
    public class Pong3 : Reply
    {
        public Pong2 result { get; set; }
    }
}

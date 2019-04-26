using RabbitMQ.TraceableMessaging.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.DTO
{
    public class Pong4 : Reply
    {
        public Pong3 Result { get; set; }

        public Pong4() { }

        public Pong4(Pong3 res) => Result = res;
    }
}

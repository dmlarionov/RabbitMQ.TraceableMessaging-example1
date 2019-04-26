﻿using RabbitMQ.TraceableMessaging.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.DTO
{
    public class Pong3 : Reply
    {
        public Pong4 Result { get; set; }

        public Pong3() { }

        public Pong3(Pong4 res) => Result = res;
    }
}

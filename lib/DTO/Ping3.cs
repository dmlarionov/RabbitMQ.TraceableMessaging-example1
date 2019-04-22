using System;
using System.Collections.Generic;
using System.Text;

namespace lib.DTO
{
    public class Ping3
    {
        public Ping2 value { get; set; }

        public Ping3() { }

        public Ping3(Ping2 ping2) => value = ping2;
    }
}
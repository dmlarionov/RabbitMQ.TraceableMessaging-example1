using System;
using System.Collections.Generic;
using System.Text;

namespace lib.DTO
{
    public class Ping3
    {
        public Ping4 Value { get; set; }

        public Ping3() { }

        public Ping3(Ping4 val) => Value = val;
    }
}
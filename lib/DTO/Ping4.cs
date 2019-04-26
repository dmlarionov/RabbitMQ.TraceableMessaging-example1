using System;
using System.Collections.Generic;
using System.Text;

namespace lib.DTO
{
    public class Ping4
    {
        public Ping3 Value { get; set; }

        public Ping4() { }

        public Ping4(Ping3 val) => Value = val;
    }
}

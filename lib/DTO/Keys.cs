using System;
using System.Collections.Generic;
using System.Text;

namespace lib.DTO
{
    /// <summary>
    /// Message for KeyDistributor
    /// </summary>
    public class Keys
    {
        public string AppInsightsInstrumKey { get; set; }
        public byte[] JwtIssuerKey { get; set; }
    }
}

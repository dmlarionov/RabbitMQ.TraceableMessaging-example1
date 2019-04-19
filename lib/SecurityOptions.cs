using Microsoft.IdentityModel.Tokens;
using RabbitMQ.TraceableMessaging.Jwt.Models;
using RabbitMQ.TraceableMessaging.Jwt.Options;
using RabbitMQ.TraceableMessaging.Models;
using RabbitMQ.TraceableMessaging.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    public class SecurityOptions : JwtSecurityOptions
    {
        private IDictionary<string, string> Map { get; set; }

        public SecurityOptions(
            IDictionary<string, string> map,
            TokenValidationParameters tokenValidationParameters) 
                : base(tokenValidationParameters)
        {
            Map = map;
            Authorize = (requestType, context) =>
            {
                string checkScope;
                if (!Map.TryGetValue(requestType, out checkScope))
                    return new AuthzResult(false, $"Scope not defined for request '{requestType}'");

                if (context.Principal.Claims.Where(c => c.Type == "scope" && c.Value == checkScope).Any())
                    return new AuthzResult(true);
                else
                    return new AuthzResult(false, $"Scope '{checkScope}' is not present in the token");
            };
        }
    }
}

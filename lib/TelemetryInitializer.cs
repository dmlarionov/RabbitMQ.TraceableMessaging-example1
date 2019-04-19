using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    public class CustomTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _serviceName;

        public CustomTelemetryInitializer(string serviceName)
            => _serviceName = serviceName;

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _serviceName;
        }
    }
}

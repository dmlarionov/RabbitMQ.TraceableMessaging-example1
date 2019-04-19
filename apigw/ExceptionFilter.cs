using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RabbitMQ.TraceableMessaging.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace apigw
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() == typeof(UnauthorizedException))
                context.Result = new UnauthorizedResult();
            else if (context.Exception.GetType() == typeof(ForbiddenException))
                context.Result = new ForbidResult();
            else
                context.Result = new StatusCodeResult(500);
        }
    }
}

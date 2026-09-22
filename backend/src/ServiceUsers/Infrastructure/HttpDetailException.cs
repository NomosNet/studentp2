using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServiceUsers.Infrastructure;

public sealed class HttpDetailException : Exception
{
    public int StatusCode { get; }

    public HttpDetailException(int statusCode, string detail) : base(detail)
    {
        StatusCode = statusCode;
    }
}

public sealed class HttpDetailExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not HttpDetailException ex)
        {
            return;
        }

        context.Result = new ObjectResult(new { detail = ex.Message })
        {
            StatusCode = ex.StatusCode
        };
        context.ExceptionHandled = true;
    }
}

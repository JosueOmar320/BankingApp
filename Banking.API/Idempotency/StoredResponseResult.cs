using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Idempotency;

/// <summary>Replays a response stored by <see cref="IdempotencyFilter"/>: same status, content type and body, plus <c>Idempotent-Replayed: true</c>.</summary>
public sealed class StoredResponseResult : IActionResult
{
    private readonly int _statusCode;
    private readonly string _contentType;
    private readonly string _body;

    /// <summary>Creates the result.</summary>
    public StoredResponseResult(int statusCode, string contentType, string body)
    {
        _statusCode = statusCode;
        _contentType = contentType;
        _body = body;
    }

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var response = context.HttpContext.Response;
        var bytes = Encoding.UTF8.GetBytes(_body);

        response.StatusCode = _statusCode;
        response.ContentType = _contentType;
        response.ContentLength = bytes.Length;
        response.Headers[IdempotencyFilter.ReplayedHeaderName] = "true";

        await response.Body.WriteAsync(bytes, context.HttpContext.RequestAborted);
    }
}

using Serilog.Context;

namespace OficinaMecanica.API.Middleware;

/// <summary>
/// Garante um identificador de correlação por requisição, propagando-o entre
/// serviços (API Gateway → Lambda → API) e enriquecendo os logs estruturados.
/// Lê o header <c>X-Correlation-ID</c> (se presente) ou gera um novo; devolve-o
/// no header de resposta e o injeta no <see cref="LogContext"/> do Serilog.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header)
            && !string.IsNullOrWhiteSpace(header)
                ? header.ToString()
                : Guid.NewGuid().ToString();

        // Disponibiliza o id para o restante do pipeline e para o log de requisição.
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

using System.Diagnostics;
using MediatR;

namespace SuperFood.Api.Common;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName,
                stopwatch.ElapsedMilliseconds);
        }
    }
}

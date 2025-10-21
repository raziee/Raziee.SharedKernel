using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Raziee.SharedKernel.CQRS;

/// <summary>
/// Pipeline behavior for logging requests and responses.
/// This behavior automatically logs request and response information.
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    /// <summary>
    /// Handles the request by logging it and the response.
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="next">The next handler in the pipeline</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The response from the next handler</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();

        _logger.LogInformation(
            "Handling {RequestName} with ID {RequestId}",
            requestName,
            requestId
        );

        try
        {
            LogRequest(request, requestId);
            var response = await next();
            LogResponse(response, requestId);

            _logger.LogInformation(
                "Successfully handled {RequestName} with ID {RequestId}",
                requestName,
                requestId
            );
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling {RequestName} with ID {RequestId}",
                requestName,
                requestId
            );
            throw;
        }
    }

    /// <summary>
    /// Logs the request information.
    /// </summary>
    /// <param name="request">The request to log</param>
    /// <param name="requestId">The request ID</param>
    private void LogRequest(TRequest request, Guid requestId)
    {
        try
        {
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogDebug("Request {RequestId}: {RequestJson}", requestId, requestJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to serialize request {RequestId} for logging",
                requestId
            );
        }
    }

    /// <summary>
    /// Logs the response information.
    /// </summary>
    /// <param name="response">The response to log</param>
    /// <param name="requestId">The request ID</param>
    private void LogResponse(TResponse response, Guid requestId)
    {
        try
        {
            var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
            _logger.LogDebug("Response {RequestId}: {ResponseJson}", requestId, responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to serialize response {RequestId} for logging",
                requestId
            );
        }
    }
}

namespace Common;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

public class CIS2AuthMiddleware : IFunctionsWorkerMiddleware
{

    private readonly ILogger<CIS2AuthMiddleware> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IAuthenticationService _authService;

    public CIS2AuthMiddleware(ILogger<CIS2AuthMiddleware> logger, ICreateResponse createResponse, IAuthenticationService authService)
    {
        _logger = logger;
        _createResponse = createResponse;
        _authService = authService;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();

        var tokenExists = AuthHelper.TryGetTokenFromHeaders(context, out var token);

        if(!tokenExists)
        {
            _logger.LogWarning("Authorization header is missing or invalid");
            var response = await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.Unauthorized, req!, "Unauthorized: Missing or invalid Authorization header.");
            context.GetInvocationResult().Value = response;
            return;
        }

        var validateToken = await _authService.ValidateTokenAsync(token);

        if(!validateToken)
        {
            _logger.LogWarning("Token validation failed");
            var response = await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.Unauthorized, req!, "Unauthorized: Invalid token.");
            context.GetInvocationResult().Value = response;
            return;
        }

        context.Items["AuthToken"] = token;
        await next(context);
    }
}

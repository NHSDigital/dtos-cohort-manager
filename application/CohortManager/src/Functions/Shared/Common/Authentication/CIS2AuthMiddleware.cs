namespace Common;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

public class Cis2AuthMiddleware : IFunctionsWorkerMiddleware
{

    private readonly ILogger<Cis2AuthMiddleware> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IAuthenticationService _authService;
    private readonly ICis2UserService _cis2UserService;

    public Cis2AuthMiddleware(ILogger<Cis2AuthMiddleware> logger, ICreateResponse createResponse, IAuthenticationService authService, ICis2UserService cis2UserService)
    {
        _logger = logger;
        _createResponse = createResponse;
        _authService = authService;
        _cis2UserService = cis2UserService;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();
        var accessToken = string.Empty;
        var tokensExist = AuthHelper.TryGetIdTokenFromHeaders(context, out var token);
        tokensExist = tokensExist && AuthHelper.TryGetAccessTokenFromHeaders(context, out accessToken);

        if(!tokensExist)
        {
            await HandleUnauthorizedAsync(context, req!, "Authorization header is missing or invalid", "Unauthorized: Missing or invalid Authorization header.");
            return;
        }

        var validateToken = await _authService.ValidateTokenAsync(token);

        if(!validateToken)
        {
            await HandleUnauthorizedAsync(context, req!, "Token validation failed", "Unauthorized: Invalid token.");
            return;
        }

        var cis2User = await _cis2UserService.GetUserFromToken(accessToken);
        if(cis2User == null)
        {
            await HandleUnauthorizedAsync(context, req!, "Failed to retrieve user from token", "Unauthorized: Failed to retrieve user from token.");
            return;
        }

        context.Items["Cis2User"] = cis2User;
        context.Items["AuthToken"] = token;
        await next(context);
    }

    private async Task HandleUnauthorizedAsync(FunctionContext context, HttpRequestData request, string logMessage, string responseMessage)
    {
        _logger.LogWarning(logMessage);
        var response = await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.Unauthorized, request, responseMessage);
        context.GetInvocationResult().Value = response;
    }
}

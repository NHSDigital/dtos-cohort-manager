namespace Common;

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;

public static class AuthHelper
{
    public static bool TryGetIdTokenFromHeaders(FunctionContext context, out string token)
    {
        return TryGetBearerTokenFromHeaders(context, "Authorization", out token);
    }

    public static bool TryGetAccessTokenFromHeaders(FunctionContext context, out string accessToken)
    {
        return TryGetBearerTokenFromHeaders(context, "X-Access-Token", out accessToken);
    }

    private static bool TryGetBearerTokenFromHeaders(FunctionContext context, string headerName, out string token)
    {
        token = null!;

        context.BindingContext.BindingData.TryGetValue("Headers", out var headersObj);

        if(headersObj is not string headersStr)
        {
            return false;
        }

        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersStr);
        if(headers == null)
        {
            return false;
        }

        if(!headers.TryGetValue(headerName, out var authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return false;
        }

        token = authHeader.Substring("Bearer ".Length).Trim();
        return true;
    }
}

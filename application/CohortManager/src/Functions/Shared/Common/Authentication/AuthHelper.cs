namespace Common;

using System.Text.Json;
using Microsoft.Azure.Functions.Worker;

public static class AuthHelper
{
    public static bool TryGetTokenFromHeaders(FunctionContext context, out string token)
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

         if(!headers.TryGetValue("Authorization", out var authHeader) || !authHeader.StartsWith("Bearer "))
         {
             return false;
         }

         token = authHeader.Substring("Bearer ".Length).Trim();
         return true;
    }
}

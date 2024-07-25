namespace NHS.CohortManager.Tests.TestUtils;

using Microsoft.Azure.Functions.Worker.Http;
using System.Text;

public class AssertionHelper
{
    public static async Task<string> ReadResponseBodyAsync(HttpResponseData responseData)
    {
        responseData.Body.Position = 0;
        using var reader = new StreamReader(responseData.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}

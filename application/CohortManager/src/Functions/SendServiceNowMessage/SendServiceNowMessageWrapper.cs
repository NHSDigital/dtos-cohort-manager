namespace NHS.CohortManager.ServiceNowMessageService;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NHS.CohortManager.ServiceNowMessageService;

public class SendServiceNowMessageFunction
{
    private readonly ServiceNowMessage _serviceNow;
    private readonly ILogger _logger;

    public SendServiceNowMessageFunction(ServiceNowMessage serviceNow, ILoggerFactory loggerFactory)
    {
        _serviceNow = serviceNow;
        _logger = loggerFactory.CreateLogger<SendServiceNowMessageFunction>();
    }

    [Function("SendServiceNowMessage")]
    public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "servicenow/send/{sysId}")] HttpRequestData req,
        string sysId)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var input = System.Text.Json.JsonSerializer.Deserialize<ServiceNowRequestModel>(requestBody);

            if (input is null || string.IsNullOrWhiteSpace(input.WorkNotes))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request payload.");
                return badRequest;
            }

            var result = await _serviceNow.SendServiceNowMessage(sysId, input.WorkNotes, input.State);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(await result.Content.ReadAsStringAsync());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending message to ServiceNow.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("ServiceNow update failed.");
            return errorResponse;
        }
    }

    private class ServiceNowRequestModel
    {
        public string WorkNotes { get; set; }
        public int State { get; set; } = 1;
    }
}

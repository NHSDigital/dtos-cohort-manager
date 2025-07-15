namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using Common;
using Data.Database;
using Azure.Messaging.ServiceBus;
public class CreateException
{
    private readonly ILogger<CreateException> _logger;
    private readonly IValidationExceptionData _validationData;
    private readonly ICreateResponse _createResponse;

    public CreateException(
        ILogger<CreateException> logger,
        IValidationExceptionData validationExceptionData,
        ICreateResponse createResponse)
    {
        _logger = logger;
        _validationData = validationExceptionData;
        _createResponse = createResponse;
    }

    [Function("CreateException")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ValidationException exception;
        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                exception = JsonSerializer.Deserialize<ValidationException>(requestBody);
            }

            if (await ProcessException(exception))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "could not create exception please see database for more details");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, ex.Message);
        }
    }

    [Function("RunCreateException")]
    public async Task Run(
      [ServiceBusTrigger(topicName: "%CreateExceptionTopic%", subscriptionName: "%ExceptionSubscription%", Connection = "ServiceBusConnectionString", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
       ServiceBusMessageActions messageActions)
    {
        try
        {
            var body = message.Body;
            var exception = JsonSerializer.Deserialize<ValidationException>(body)!;

            if (!await ProcessException(exception!))
            {
                _logger.LogError("could not create exception please see database for more details");
                await messageActions.DeadLetterMessageAsync(message);
                return;
            }

            await messageActions.CompleteMessageAsync(message);
            _logger.LogInformation("added exception to database and completed message successfully ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "exception could not be added to service bus topic. See dead letter storage for more: {ExceptionMessage}", ex.Message);
            await messageActions.DeadLetterMessageAsync(message);

        }
    }


    private async Task<bool> ProcessException(ValidationException exception)
    {
        if (await _validationData.Create(exception))
        {
            _logger.LogInformation("The exception record has been created successfully");
            return true;
        }
        return false;
    }
}

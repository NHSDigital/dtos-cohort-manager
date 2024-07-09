namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using System.Runtime.CompilerServices;
using Common;

public class CreateException
    {
        private readonly ILogger<CreateException> _logger;

        private readonly ICallFunction _callFunction;
        public CreateException(ILogger<CreateException> logger, ICallFunction callFunction)
        {
            _logger = logger;
            _callFunction = callFunction;
        }

        [Function("CreateException")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {

            ValidationException validationException;

            try{
                string requestBody;

                using (var reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if(String.IsNullOrEmpty(requestBody)){
                    _logger.LogError("CreateException received an empty payload");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                validationException = JsonSerializer.Deserialize<ValidationException>(requestBody);

                var jsonString  = JsonSerializer.Serialize<ValidationException>(validationException);
                var createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("CreateValidationExceptionURL"),jsonString);

                if(createResponse.StatusCode != HttpStatusCode.OK){
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }
                return req.CreateResponse(HttpStatusCode.OK);



            }
            catch(Exception ex){
                _logger.LogError(ex,"there has been an error while creating an exception record: {Message}", ex.Message);
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
    }


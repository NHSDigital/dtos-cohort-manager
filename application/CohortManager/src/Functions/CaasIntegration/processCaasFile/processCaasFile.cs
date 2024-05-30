using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace processCaasFile
{
    public class ProcessCaasFileFunction
    {
        private readonly ILogger<ProcessCaasFileFunction> _logger;
        private readonly ICallFunction _callFunction;
        private readonly ICreateResponse _createResponse;

        public ProcessCaasFileFunction(ILogger<ProcessCaasFileFunction> logger, ICallFunction callFunction, ICreateResponse createResponse)
        {
            _logger = logger;
            _callFunction = callFunction;
            _createResponse = createResponse;
        }

        [Function("processCaasFile")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {

            string postdata = "";
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                postdata = reader.ReadToEnd();
            }
            Cohort input = JsonSerializer.Deserialize<Cohort>(postdata);

            // debug info
            _logger.LogInformation("Records received: \n" + input.Participants.Count);

            _logger.LogInformation($"Records received {input.Participants.Count}");
            int add = 0, upd = 0, del = 0, err = 0, row = 0;

            foreach (Participant p in input.Participants)
            {
                row++;
                switch (p.RecordType.Trim())
                {
                    case Actions.New:
                        add++;
                        try
                        {
                            var json = JsonSerializer.Serialize(p);
                            var addresp = await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSAddParticipant"), json);
                            _logger.LogInformation("Called add participant");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                        }
                        break;
                    case Actions.Amended:
                        upd++;
                        try
                        {
                            var json = JsonSerializer.Serialize(p);
                            var addresp = await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSUpdateParticipant"), json);
                            _logger.LogInformation("Called update participant");

                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                        }
                        break;
                    case Actions.Removed:
                        del++;
                        try
                        {
                            var json = JsonSerializer.Serialize(p);
                            var addresp = await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSRemoveParticipant"), json);
                            _logger.LogInformation("Called remove participant");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                        }
                        break;
                    default:
                        err++;
                        //_logger.LogInformation($"error row:{row} action :{p.action}");
                        break;
                }
            }

            _logger.LogInformation($"There are {add} Additions. There are {upd} Updates. There are {del} Deletions. There are {err} Errors.");


            //send to eventgrid
            /*
            EventGridPublisherClient client = new EventGridPublisherClient(
            new Uri(),
            new AzureKeyCredential());

            // Add EventGridEvents to a list to publish to the topic
            EventGridEvent egEvent =
                new EventGridEvent(
                    "ExampleEventSubject",
                    "Example.EventType",
                    "1.0",
                    "Hello world!");

            try
            {
                // Send the event
                client.SendEvent(egEvent);
                _logger.LogInformation("test event sent.");
            }
            catch(Exception ex)
            {
                _logger.LogInformation($"Unable to send event.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
            }
            */

            // set response headers and return

            if (err > 0)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
    }
}

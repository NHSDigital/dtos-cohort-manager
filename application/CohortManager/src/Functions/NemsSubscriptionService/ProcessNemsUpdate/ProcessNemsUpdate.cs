﻿namespace NHS.Screening.ProcessNemsUpdate;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ProcessNemsUpdate
{
    private readonly ILogger<ProcessNemsUpdate> _logger;
    private readonly ProcessNemsUpdateConfig _config;

    public ProcessNemsUpdate(
        ILogger<ProcessNemsUpdate> logger,
        IOptions<ProcessNemsUpdateConfig> processNemsUpdateConfig)
    {
        _logger = logger;
        _config = processNemsUpdateConfig.Value;
    }

    [Function(nameof(ProcessNemsUpdate))]
    public async Task Run([BlobTrigger("nems-messages/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string name)
    {
        _logger.LogInformation("ProcessNemsUpdate function triggered.");
        _logger.LogInformation("This function will use the following config: {Url}", _config.RetrievePdsDemographicURL);
    }
}

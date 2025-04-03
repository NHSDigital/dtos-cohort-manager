namespace Common;

using Common.Interfaces;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Model;

public class FhirParserHelper : IFhirParserHelper
{
    private readonly ILogger<FhirParserHelper> _logger;
    public FhirParserHelper(ILogger<FhirParserHelper> logger)
    {
        _logger = logger;
    }

    public Demographic FhirParser(string json)
    {
        var parser = new FhirJsonParser();

        try
        {
            var parsedPatient = parser.Parse<Patient>(json);
            return new Demographic(parsedPatient);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to parse FHIR json");
            throw;
        }
    }
}

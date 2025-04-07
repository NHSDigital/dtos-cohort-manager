namespace Common.Interfaces;

using Hl7.Fhir.Model;
using Model;

public interface IFhirParserHelper
{
    /// <summary>
    /// Parses FHIR JSON and converts it to a Demographic object
    /// </summary>
    /// <param name="json">The FHIR JSON string</param>
    /// <returns>A Demographic model populated from FHIR data</returns>
    PDSDemographic ParseFhirJson(string json);

    /// <summary>
    /// Maps a FHIR Patient object to a new Demographic object
    /// </summary>
    /// <param name="patient">The FHIR Patient object</param>
    /// <returns>A new Demographic model populated from FHIR data</returns>
    PDSDemographic MapPatientToPDSDemographic(Patient patient);
}

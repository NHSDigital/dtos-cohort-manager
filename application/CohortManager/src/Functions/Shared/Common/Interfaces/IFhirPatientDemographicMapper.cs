namespace Common.Interfaces;

using Hl7.Fhir.Model;
using Model;

public interface IFhirPatientDemographicMapper
{
    /// <summary>
    /// Parses FHIR JSON and converts it to a Demographic object
    /// </summary>
    /// <param name="json">The FHIR JSON string</param>
    /// <returns>A Demographic model populated from FHIR data</returns>
    PdsDemographic ParseFhirJson(string json);

    /// <summary>
    /// Parses FHIR JSON NHS number and returns it
    /// </summary>
    /// <param name="json">The FHIR JSON string</param>
    /// <returns>The NHS number as a string</returns>
    string ParseFhirJsonNhsNumber(string json);

    /// <summary>
    /// Parses FHIR XML NHS number and returns it
    /// </summary>
    /// <param name="xml">The FHIR XML string</param>
    /// <returns>The NHS number as a string</returns>
    string ParseFhirXmlNhsNumber(string xml);

    /// <summary>
    /// Maps a FHIR Patient object to a new Demographic object
    /// </summary>
    /// <param name="patient">The FHIR Patient object</param>
    /// <returns>A new Demographic model populated from FHIR data</returns>
    PdsDemographic MapPatientToPDSDemographic(Patient patient);
}

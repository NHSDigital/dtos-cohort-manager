namespace Common;

using CohortManager.Functions.Shared.Common;
using Common.Interfaces;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Xml;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using System;
using System.Linq;

public class FhirPatientDemographicMapper : IFhirPatientDemographicMapper
{
    private readonly ILogger<FhirPatientDemographicMapper> _logger;

    public FhirPatientDemographicMapper(ILogger<FhirPatientDemographicMapper> logger)
    {
        _logger = logger;
    }

    public PdsDemographic ParseFhirJson(string json)
    {
        var parser = new FhirJsonParser();
        try
        {
            var parsedPatient = parser.Parse<Patient>(json);
            return MapPatientToPDSDemographic(parsedPatient);
        }
        catch (FormatException ex)
        {
            var errorMessage = "Failed to parse FHIR json. Ensure the input is a valid FHIR Patient resource.";
            _logger.LogError(ex, "{Message}", errorMessage);
            throw new FormatException(errorMessage, ex);
        }
    }

    public string ParseFhirJsonNhsNumber(string json)
    {
        var parser = new FhirJsonParser();
        try
        {
            var parsedPatient = parser.Parse<Patient>(json);
            return ExtractNhsNumberFromPatient(parsedPatient);
        }
        catch (FormatException ex)
        {
            var errorMessage = "Failed to parse FHIR json NHS number. Ensure the input is a valid FHIR Patient resource.";
            _logger.LogError(ex, "{Message}", errorMessage);
            throw new FormatException(errorMessage, ex);
        }
    }

    public string ParseFhirXmlNhsNumber(string xml)
    {
        try
        {
            // For Bundle format (NEMS), extract Patient XML first then parse
            if (xml.Contains("<Bundle"))
            {
                var patientXml = ExtractPatientXmlFromBundle(xml);
                if (string.IsNullOrEmpty(patientXml))
                    return string.Empty;
                
                var parser = new FhirXmlParser();
                var patient = parser.Parse<Patient>(patientXml);
                return ExtractNhsNumberFromPatient(patient);
            }
            else
            {
                var parser = new FhirXmlParser();
                var patient = parser.Parse<Patient>(xml);
                return ExtractNhsNumberFromPatient(patient);
            }
        }
        catch (FormatException ex)
        {
            var errorMessage = "Failed to parse FHIR XML NHS number. Ensure the input is a valid FHIR Patient resource or Bundle.";
            _logger.LogError(ex, "{Message}", errorMessage);
            throw new FormatException(errorMessage, ex);
        }
    }

    private static string ExtractPatientXmlFromBundle(string bundleXml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(bundleXml);
        
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("fhir", "http://hl7.org/fhir");
        
        var patientNode = doc.SelectSingleNode("//fhir:Patient", nsManager);
        return patientNode?.OuterXml ?? string.Empty;
    }


    private static string ExtractNhsNumberFromPatient(Patient patient)
    {
        if (patient?.Identifier == null) return patient?.Id ?? string.Empty;

        // Look for NHS number identifier
        var nhsIdentifier = patient.Identifier.FirstOrDefault(id => 
            id.System == "https://fhir.nhs.uk/Id/nhs-number");

        return nhsIdentifier?.Value ?? patient.Id ?? string.Empty;
    }

    public PdsDemographic MapPatientToPDSDemographic(Patient patient)
    {
        var demographic = new PdsDemographic();

        if (patient == null)
        {
            throw new ArgumentNullException(nameof(patient));
        }

        // Basic Identifiers
        demographic.NhsNumber = patient.Id; // We set to PDS NHS even if different from request
        demographic.ParticipantId = null; // We do not know the Participant ID from PDS
        demographic.RecordUpdateDateTime = null; // We do not know the RecordUpdateDateTime from PDS
        demographic.RecordInsertDateTime = null; // We do not know the RecordInsertDateTime from PDS
        demographic.DateOfBirth = patient.BirthDate;

        // CurrentPosting & CurrentPostingEffectiveFromDate
        // these are not set as not available in PDS

        MapPrimaryCareProvider(patient, demographic);
        MapNames(patient, demographic);
        MapGender(patient, demographic);
        MapAddress(patient, demographic);
        MapDeathInformation(patient, demographic);
        MapContactInformation(patient, demographic);
        MapLanguagePreferences(patient, demographic);
        MapRemovalInformation(patient, demographic);
        MapSecurityMetadata(patient, demographic);

        return demographic;
    }

    private static void MapPrimaryCareProvider(Patient patient, Demographic demographic)
    {
        if (patient.GeneralPractitioner == null || patient.GeneralPractitioner.Count == 0)
            return;

        var gp = patient.GeneralPractitioner[0];
        if (gp.Identifier == null)
            return;

        demographic.PrimaryCareProvider = gp.Identifier.Value;

        if (gp.Identifier.Period?.Start != null)
            demographic.PrimaryCareProviderEffectiveFromDate = gp.Identifier.Period.Start.ToString();
    }

    private static void MapNames(Patient patient, Demographic demographic)
    {
        // Look for a previous name (maiden name or old name) to populate previous family name
        var previousName = patient.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Maiden || n.Use == HumanName.NameUse.Old);
        if (previousName != null)
        {
            demographic.PreviousFamilyName = previousName.Family;  // Store previous surname
        }

        // First try to get the "usual" name if available, otherwise take the first name in the list
        var usualName = patient.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Usual)
            ?? patient.Name?.FirstOrDefault();

        // If no name is found, return early
        if (usualName == null)
            return;

        // Map individual name components from the usual name to the demographic object
        demographic.NamePrefix = usualName.Prefix?.FirstOrDefault();  // Title/prefix (e.g., "Mr.", "Dr.")
        demographic.FirstName = usualName.Given?.FirstOrDefault();    // Primary given name

        // Handle middle names or additional given names if present
        if (usualName.Given != null && usualName.Given.Count() > 1)
        {
            // Combine all given names other than their first one, separated with spaces
            demographic.OtherGivenNames = string.Join(" ", usualName.Given.Skip(1).ToArray());
        }

        demographic.FamilyName = usualName.Family;  // Family/surname
    }

    private static void MapGender(Patient patient, Demographic demographic)
    {
        // Gender mapping to our enum
        if (patient.Gender.HasValue)
        {
            switch (patient.Gender.Value)
            {
                case AdministrativeGender.Male:
                    demographic.Gender = Gender.Male;
                    break;
                case AdministrativeGender.Female:
                    demographic.Gender = Gender.Female;
                    break;
                case AdministrativeGender.Other:
                    demographic.Gender = Gender.NotSpecified;
                    break;
                default:
                    demographic.Gender = Gender.NotKnown;
                    break;
            }
        }
    }

    private static void MapAddress(Patient patient, Demographic demographic)
    {
        // Find the home address or first available address
        var homeAddress = GetHomeAddress(patient);
        if (homeAddress == null)
        {
            return;
        }

        // Map all address components
        MapAddressComponents(homeAddress, demographic);
    }

    private static Address? GetHomeAddress(Patient patient)
    {
        return patient.Address?.FirstOrDefault(a => a.Use == Address.AddressUse.Home)
            ?? patient.Address?.FirstOrDefault();
    }

    private static void MapAddressComponents(Address address, Demographic demographic)
    {
        // Map address lines
        if (address.Line != null)
        {
            var addressLines = address.Line.ToArray();
            demographic.AddressLine1 = addressLines.Length > 0 ? addressLines[0] : null;
            demographic.AddressLine2 = addressLines.Length > 1 ? addressLines[1] : null;
            demographic.AddressLine3 = addressLines.Length > 2 ? addressLines[2] : null;
            demographic.AddressLine4 = addressLines.Length > 3 ? addressLines[3] : null;
            demographic.AddressLine5 = addressLines.Length > 4 ? addressLines[4] : null;
        }

        // Map other address details
        demographic.Postcode = address.PostalCode;
        MapPafKeyFromExtensions(address, demographic);

        // Map effective date
        if (address.Period?.Start != null)
        {
            demographic.UsualAddressEffectiveFromDate = address.Period.Start.ToString();
        }
    }

    private static void MapPafKeyFromExtensions(Address homeAddress, Demographic demographic)
    {
        if (homeAddress?.Extension == null)
            return;

        var pafExtension = homeAddress.Extension.FirstOrDefault(e =>
            e.Url == FhirExtensionUrls.UkCoreAddressKey);
        if (pafExtension == null || pafExtension.Extension == null)
            return;

        var typeExtension = pafExtension.Extension.FirstOrDefault(e => e.Url == "type");
        var valueExtension = pafExtension.Extension.FirstOrDefault(e => e.Url == "value");

        // Verify that the address key extensions contain valid FHIR data:
        // 1. Type extension must contain a Coding value with PAF code
        // 2. Value extension must contain a FhirString value
        // Both conditions must be met to extract a valid PAF (Postcode Address File) key
        if (typeExtension?.Value is not Coding typeCoding ||
            typeCoding.Code != AddressKeyTypes.Paf ||
            valueExtension?.Value is not FhirString valueString)
        {
            return;
        }

        demographic.PafKey = valueString.Value;
    }

    private static void MapDeathInformation(Patient patient, Demographic demographic)
    {
        // Death Date
        if (patient.Deceased is FhirDateTime deceasedDate)
        {
            demographic.DateOfDeath = deceasedDate.Value;
        }

        // Death notification status
        var deathNotificationExtension = patient.Extension?.FirstOrDefault(e =>
            e.Url == FhirExtensionUrls.UkCoreDeathNotificationStatus);

        if (deathNotificationExtension == null)
            return;

        var statusExtension = deathNotificationExtension.Extension?
            .FirstOrDefault(e => e.Url == "deathNotificationStatus");

        if (statusExtension?.Value is not CodeableConcept codeableConcept)
            return;

        var coding = codeableConcept.Coding?.FirstOrDefault();
        if (coding == null || !short.TryParse(coding.Code, out short statusCode))
            return;

        demographic.DeathStatus = Enum.IsDefined(typeof(Status), statusCode)
            ? (Status)statusCode
            : null;
    }

    private static void MapContactInformation(Patient patient, Demographic demographic)
    {
        if (patient.Telecom == null)
        {
            return;
        }

        // Home phone
        var homePhone = patient.Telecom.FirstOrDefault(t =>
            t.System == ContactPoint.ContactPointSystem.Phone &&
            t.Use == ContactPoint.ContactPointUse.Home);
        if (homePhone != null)
        {
            MapContactPoint(homePhone,
                value => demographic.TelephoneNumber = value,
                date => demographic.TelephoneNumberEffectiveFromDate = date);
        }

        // Mobile phone
        var mobilePhone = patient.Telecom.FirstOrDefault(t =>
            t.System == ContactPoint.ContactPointSystem.Phone &&
            t.Use == ContactPoint.ContactPointUse.Mobile);
        if (mobilePhone != null)
        {
            MapContactPoint(mobilePhone,
                value => demographic.MobileNumber = value,
                date => demographic.MobileNumberEffectiveFromDate = date);
        }

        // Email
        var email = patient.Telecom.FirstOrDefault(t =>
            t.System == ContactPoint.ContactPointSystem.Email &&
            t.Use == ContactPoint.ContactPointUse.Home);
        if (email != null)
        {
            MapContactPoint(email,
                value => demographic.EmailAddress = value,
                date => demographic.EmailAddressEffectiveFromDate = date);
        }
    }

    private static void MapContactPoint(
        ContactPoint contactPoint,
        Action<string> setValueAction,
        Action<string> setDateAction)
    {
        // Now we know contactPoint isn't null when this method is called
        setValueAction(contactPoint.Value);

        if (contactPoint.Period?.Start != null)
        {
            setDateAction(contactPoint.Period.Start.ToString());
        }
    }

    private static void MapLanguagePreferences(Patient patient, Demographic demographic)
    {
        var languageExtension = patient.Extension?.FirstOrDefault(e =>
            e.Url == FhirExtensionUrls.UkCoreNhsCommunication);

        if (languageExtension != null)
        {
            var languageComponent = languageExtension.Extension?.FirstOrDefault(e => e.Url == "language");
            var interpreterComponent = languageExtension.Extension?.FirstOrDefault(e => e.Url == "interpreterRequired");

            if (languageComponent != null)
            {
                var languageCoding = (languageComponent.Value as CodeableConcept)?.Coding?.FirstOrDefault();
                demographic.PreferredLanguage = languageCoding?.Display ?? languageCoding?.Code;
            }

            if (interpreterComponent != null && interpreterComponent.Value is FhirBoolean interpreterBool && interpreterBool.Value.HasValue)
            {
                demographic.IsInterpreterRequired = interpreterBool.Value.Value.ToString();
            }
        }
        else
        {
            // we can assume that if the language extension is not returned then the person does not require an interpreter
            demographic.IsInterpreterRequired = "false";
        }
    }

    private static void MapRemovalInformation(Patient patient, PdsDemographic demographic)
    {
        // Find the removal from registration extension
        var removalExtension = patient.Extension?.FirstOrDefault(e =>
            e.Url == FhirExtensionUrls.PdsRemovalFromRegistration);

        if (removalExtension == null)
            return;

        // Map the removal reason code
        var removalCodeExtension = removalExtension.Extension?.FirstOrDefault(e =>
            e.Url == "removalFromRegistrationCode");

        if (removalCodeExtension?.Value is CodeableConcept removalConcept)
        {
            var removalCoding = removalConcept.Coding?.FirstOrDefault();
            if (removalCoding != null)
            {
                // Set the removal reason to the code value only, no fallback
                demographic.ReasonForRemoval = removalCoding.Code;
            }
        }

        // Map the effective time period
        var effectiveTimeExtension = removalExtension.Extension?.FirstOrDefault(e =>
            e.Url == "effectiveTime");

        if (effectiveTimeExtension?.Value is not Period effectivePeriod)
            return;

        if (effectivePeriod.Start != null)
        {
            demographic.RemovalEffectiveFromDate = effectivePeriod.Start.ToString();
        }

        if (effectivePeriod.End != null)
        {
            demographic.RemovalEffectiveToDate = effectivePeriod.End.ToString();
        }
    }

    private static void MapSecurityMetadata(Patient patient, PdsDemographic demographic)
    {
        // Check if the patient has security metadata
        if (patient.Meta?.Security == null || !patient.Meta.Security.Any())
        {
            return;
        }

        // Look for the confidentiality code
        var confidentialityCoding = patient.Meta.Security.FirstOrDefault(s =>
            s.System == FhirExtensionUrls.V3Confidentiality);

        // If no confidentiality code is found, return early
        if (confidentialityCoding == null)
        {
            return;
        }

        demographic.ConfidentialityCode = confidentialityCoding.Code;
    }
}

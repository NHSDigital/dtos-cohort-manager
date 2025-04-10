namespace Common;

using Common.Interfaces;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using System;
using System.Linq;

public class FhirPatientDemographicMapper : IFhirPatientDemographicMapper
{
    private readonly ILogger<FhirPatientDemographicMapper> _logger;

    // URL constants for FHIR extensions
    private static class FhirExtensionUrls
    {
        public const string UkCoreAddressKey = "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-AddressKey";
        public const string UkCoreDeathNotificationStatus = "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-DeathNotificationStatus";
        public const string UkCoreNhsCommunication = "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-NHSCommunication";
        public const string PdsRemovalFromRegistration = "https://fhir.nhs.uk/StructureDefinition/Extension-PDS-RemovalFromRegistration";
        public const string V3Confidentiality = "http://terminology.hl7.org/CodeSystem/v3-Confidentiality";
    }

    public FhirPatientDemographicMapper(ILogger<FhirPatientDemographicMapper> logger)
    {
        _logger = logger;
    }

    public PDSDemographic ParseFhirJson(string json)
    {
        var parser = new FhirJsonParser();
        try
        {
            var parsedPatient = parser.Parse<Patient>(json);
            return MapPatientToPDSDemographic(parsedPatient);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to parse FHIR json");
            throw;
        }
    }

    public PDSDemographic MapPatientToPDSDemographic(Patient patient)
    {
        var demographic = new PDSDemographic();

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
        if (patient.GeneralPractitioner != null && patient.GeneralPractitioner.Count > 0)
        {
            var gp = patient.GeneralPractitioner[0];
            if (gp.Identifier != null)
            {
                demographic.PrimaryCareProvider = gp.Identifier.Value;

                if (gp.Identifier.Period?.Start != null)
                {
                    demographic.PrimaryCareProviderEffectiveFromDate = gp.Identifier.Period.Start.ToString();
                }
            }
        }
    }

    private static void MapNames(Patient patient, Demographic demographic)
    {
        var usualName = patient.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Usual)
            ?? patient.Name?.FirstOrDefault();

        if (usualName != null)
        {
            demographic.NamePrefix = usualName.Prefix?.FirstOrDefault();
            demographic.FirstName = usualName.Given?.FirstOrDefault();

            // Other given names (if more than one given name exists)
            if (usualName.Given != null && usualName.Given.Count() > 1)
            {
                demographic.OtherGivenNames = string.Join(" ", usualName.Given.Skip(1).ToArray());
            }

            demographic.FamilyName = usualName.Family;
        }

        // Previous family name (maiden or old)
        var previousName = patient.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Maiden || n.Use == HumanName.NameUse.Old);
        if (previousName != null)
        {
            demographic.PreviousFamilyName = previousName.Family;
        }
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
        var pafExtension = homeAddress?.Extension?.FirstOrDefault(e =>
            e.Url == FhirExtensionUrls.UkCoreAddressKey);

        if (pafExtension != null)
        {
            var typeExtension = pafExtension.Extension?.FirstOrDefault(e => e.Url == "type");
            var valueExtension = pafExtension.Extension?.FirstOrDefault(e => e.Url == "value");

            if (typeExtension?.Value is Coding typeCoding &&
                typeCoding.Code == "PAF" &&
                valueExtension?.Value is FhirString valueString)
            {
                demographic.PafKey = valueString.Value;
            }
        }
    }

    private static void MapDeathInformation(Patient patient, Demographic demographic)
    {
        // Death Date
        if (patient.Deceased != null)
        {
            if (patient.Deceased is FhirDateTime deceasedDate)
            {
                demographic.DateOfDeath = deceasedDate.Value;
            }
        }

        // Death notification status
        var deathNotificationExtension = patient.Extension?.FirstOrDefault(e =>
            e.Url == FhirExtensionUrls.UkCoreDeathNotificationStatus);

        if (deathNotificationExtension != null)
        {
            var statusExtension = deathNotificationExtension.Extension?.FirstOrDefault(e => e.Url == "deathNotificationStatus");
            if (statusExtension?.Value is CodeableConcept codeableConcept)
            {
                var coding = codeableConcept.Coding?.FirstOrDefault();
                if (coding != null)
                {
                    switch (coding.Code)
                    {
                        case "2": // "Formal - death notice received from Registrar of Deaths"
                            demographic.DeathStatus = Status.Formal;
                            break;
                        case "1": // "Informal - death notice received via relative or other source"
                            demographic.DeathStatus = Status.Informal;
                            break;
                        default:
                            demographic.DeathStatus = null;
                            break;
                    }
                }
            }
        }
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
    }

    private static void MapRemovalInformation(Patient patient, PDSDemographic demographic)
    {
        // Find the removal from registration extension
        var removalExtension = patient.Extension?.FirstOrDefault(e =>
            e.Url == FhirExtensionUrls.PdsRemovalFromRegistration);

        if (removalExtension != null)
        {
            // Map the removal reason code only
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

            if (effectiveTimeExtension?.Value is Period effectivePeriod)
            {
                if (effectivePeriod.Start != null)
                {
                    demographic.EffectiveFromDate = effectivePeriod.Start.ToString();
                }

                if (effectivePeriod.End != null)
                {
                    demographic.EffectiveToDate = effectivePeriod.End.ToString();
                }
            }
        }
    }

    private static void MapSecurityMetadata(Patient patient, PDSDemographic demographic)
    {
        // Check if the patient has security metadata
        if (patient.Meta?.Security != null && patient.Meta.Security.Any())
        {
            // Look for the confidentiality code
            var confidentialityCoding = patient.Meta.Security.FirstOrDefault(s =>
                s.System == FhirExtensionUrls.V3Confidentiality);

            if (confidentialityCoding != null)
            {
                demographic.ConfidentialityCode = confidentialityCoding.Code;
            }
        }
    }
}
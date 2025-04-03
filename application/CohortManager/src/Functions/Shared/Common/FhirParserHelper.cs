namespace Common;

using Common.Interfaces;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using System;
using System.Linq;

public class FhirParserHelper : IFhirParserHelper
{
    private readonly ILogger<FhirParserHelper> _logger;

    public FhirParserHelper(ILogger<FhirParserHelper> logger)
    {
        _logger = logger;
    }

    public Demographic ParseFhirJson(string json)
    {
        var parser = new FhirJsonParser();
        try
        {
            var parsedPatient = parser.Parse<Patient>(json);
            return MapPatientToDemographic(parsedPatient);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to parse FHIR json");
            throw;
        }
    }

    public Demographic MapPatientToDemographic(Patient patient)
    {
        var demographic = new Demographic();

        if (patient == null)
            throw new ArgumentNullException(nameof(patient));

        // Basic Identifiers
        demographic.NhsNumber = patient.Id;
        // TODO: should participant ID be set here or no? Set to null for now
        demographic.ParticipantId = null; // We do not know the Participant ID as there may not be one
        demographic.RecordUpdateDateTime = patient.Meta?.LastUpdated?.ToString(); // TODO: should this be null as usually refers to participant?
        demographic.RecordInsertDateTime = null; // TODO: No clear source for initial creation date in FHIR
        demographic.DateOfBirth = patient.BirthDate;

        //TODO: Superseded NHS Number - how does CM manage?

        //TODO: CurrentPosting & CurrentPostingEffectiveFromDate - what should these be set to?

        MapPrimaryCareProvider(patient, demographic);
        MapNames(patient, demographic);
        MapGender(patient, demographic);
        MapAddress(patient, demographic);
        MapDeathInformation(patient, demographic);
        MapContactInformation(patient, demographic);
        MapLanguagePreferences(patient, demographic);

        // TODO: Reason for Removal and effective date - not sure where to put these as not in demographic model
        // they are mentioned in confluence

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
        if (homeAddress == null) return;

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
            e.Url == "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-AddressKey");

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
            e.Url == "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-DeathNotificationStatus");

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
        if (patient.Telecom == null) return;

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
            e.Url == "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-NHSCommunication");

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
}

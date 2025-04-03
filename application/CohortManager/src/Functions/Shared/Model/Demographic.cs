namespace Model;

using System;
using System.Linq;
using Hl7.Fhir.Model;
using Model.Enums;

public class Demographic
{
    public string? ParticipantId { get; set; }
    public string? NhsNumber { get; set; }
    public string? SupersededByNhsNumber { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public string? PrimaryCareProviderEffectiveFromDate { get; set; }
    public string? CurrentPosting { get; set; }
    public string? CurrentPostingEffectiveFromDate { get; set; }
    public string? NamePrefix { get; set; }
    public string? FirstName { get; set; }
    public string? OtherGivenNames { get; set; }
    public string? FamilyName { get; set; }
    public string? PreviousFamilyName { get; set; }
    public string? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? AddressLine5 { get; set; }
    public string? Postcode { get; set; }
    public string? PafKey { get; set; }
    public string? UsualAddressEffectiveFromDate { get; set; }
    public string? DateOfDeath { get; set; }
    public Status? DeathStatus { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? TelephoneNumberEffectiveFromDate { get; set; }
    public string? MobileNumber { get; set; }
    public string? MobileNumberEffectiveFromDate { get; set; }
    public string? EmailAddress { get; set; }
    public string? EmailAddressEffectiveFromDate { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? IsInterpreterRequired { get; set; }
    public string? InvalidFlag { get; set; }
    public string? RecordInsertDateTime { get; set; }
    public string? RecordUpdateDateTime { get; set; }
    public Demographic() { }

    public Demographic(Patient patient)
    {
        if (patient == null)
            throw new ArgumentNullException(nameof(patient));

        // Basic Identifiers
        NhsNumber = patient.Id;

        // TODO: should participant ID be set here or no? gone with same as NHS number for now
        ParticipantId = patient.Identifier?.FirstOrDefault(i => i.System == "https://fhir.nhs.uk/Id/nhs-number")?.Value ?? patient.Id;

        RecordUpdateDateTime = patient.Meta?.LastUpdated?.ToString(); // When the record was last updated TODO: should this be null?
        RecordInsertDateTime = null; // TODO: No clear source for initial creation date in FHIR

        // Primary Care Provider (GP)
        if (patient.GeneralPractitioner != null && patient.GeneralPractitioner.Count > 0)
        {
            var gp = patient.GeneralPractitioner[0];
            if (gp.Identifier != null)
            {
                PrimaryCareProvider = gp.Identifier.Value;

                if (gp.Identifier.Period?.Start != null)
                {
                    PrimaryCareProviderEffectiveFromDate = gp.Identifier.Period.Start.ToString();
                }
            }
        }

        // Name information
        var usualName = patient.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Usual)
            ?? patient.Name?.FirstOrDefault();

        if (usualName != null)
        {
            NamePrefix = usualName.Prefix?.FirstOrDefault();
            FirstName = usualName.Given?.FirstOrDefault();

            // Other given names (if more than one given name exists)
            if (usualName.Given != null && usualName.Given.Count() > 1)
            {
                OtherGivenNames = string.Join(" ", usualName.Given.Skip(1).ToArray());
            }

            FamilyName = usualName.Family;
        }

        // Previous family name (maiden or old)
        var previousName = patient.Name?.FirstOrDefault(n => n.Use == HumanName.NameUse.Maiden || n.Use == HumanName.NameUse.Old);
        if (previousName != null)
        {
            PreviousFamilyName = previousName.Family;
        }

        // Date of Birth
        DateOfBirth = patient.BirthDate;

        // Gender mapping to our enum
        if (patient.Gender.HasValue)
        {
            switch (patient.Gender.Value)
            {
                case AdministrativeGender.Male:
                    this.Gender = Model.Enums.Gender.Male;
                    break;
                case AdministrativeGender.Female:
                    this.Gender = Model.Enums.Gender.Female;
                    break;
                case AdministrativeGender.Other:
                    this.Gender = Model.Enums.Gender.NotSpecified;
                    break;
                case AdministrativeGender.Unknown:
                default:
                    this.Gender = Model.Enums.Gender.NotKnown;
                    break;
            }
        }

        // Address information
        var homeAddress = patient.Address?.FirstOrDefault(a => a.Use == Address.AddressUse.Home)
            ?? patient.Address?.FirstOrDefault();

        if (homeAddress != null)
        {
            if (homeAddress.Line != null)
            {
                var addressLines = homeAddress.Line.ToArray();
                AddressLine1 = addressLines.Length > 0 ? addressLines[0] : null;
                AddressLine2 = addressLines.Length > 1 ? addressLines[1] : null;
                AddressLine3 = addressLines.Length > 2 ? addressLines[2] : null;
                AddressLine4 = addressLines.Length > 3 ? addressLines[3] : null;
                AddressLine5 = addressLines.Length > 4 ? addressLines[4] : null;
            }

            Postcode = homeAddress.PostalCode;

            // PAF Key from extensions
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
                    PafKey = valueString.Value;
                }
            }

            // Address effective date
            if (homeAddress.Period?.Start != null)
            {
                UsualAddressEffectiveFromDate = homeAddress.Period.Start.ToString();
            }
        }

        // Death Date
        if (patient.Deceased != null)
        {
            if (patient.Deceased is FhirDateTime deceasedDate)
            {
                DateOfDeath = deceasedDate.Value;
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
                            this.DeathStatus = Model.Enums.Status.Formal;
                            break;
                        case "1": // "Informal - death notice received via relative or other source"
                            this.DeathStatus = Model.Enums.Status.Informal;
                            break;
                        default:
                            this.DeathStatus = null;
                            break;
                    }
                }
            }
        }

        // Contact information
        if (patient.Telecom != null)
        {
            // Home phone
            var homePhone = patient.Telecom.FirstOrDefault(t =>
                t.System == ContactPoint.ContactPointSystem.Phone &&
                t.Use == ContactPoint.ContactPointUse.Home);

            if (homePhone != null)
            {
                TelephoneNumber = homePhone.Value;
                if (homePhone.Period?.Start != null)
                {
                    TelephoneNumberEffectiveFromDate = homePhone.Period.Start.ToString();
                }
            }

            // Mobile phone
            var mobilePhone = patient.Telecom.FirstOrDefault(t =>
                t.System == ContactPoint.ContactPointSystem.Phone &&
                t.Use == ContactPoint.ContactPointUse.Mobile);

            if (mobilePhone != null)
            {
                MobileNumber = mobilePhone.Value;
                if (mobilePhone.Period?.Start != null)
                {
                    MobileNumberEffectiveFromDate = mobilePhone.Period.Start.ToString();
                }
            }

            // Email
            var email = patient.Telecom.FirstOrDefault(t =>
                t.System == ContactPoint.ContactPointSystem.Email &&
                t.Use == ContactPoint.ContactPointUse.Home);

            if (email != null)
            {
                EmailAddress = email.Value;
                if (email.Period?.Start != null)
                {
                    EmailAddressEffectiveFromDate = email.Period.Start.ToString();
                }
            }
        }

        // Language preferences
        var languageExtension = patient.Extension?.FirstOrDefault(e =>
            e.Url == "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-NHSCommunication");

        if (languageExtension != null)
        {
            var languageComponent = languageExtension.Extension?.FirstOrDefault(e => e.Url == "language");
            var interpreterComponent = languageExtension.Extension?.FirstOrDefault(e => e.Url == "interpreterRequired");

            if (languageComponent != null)
            {
                var languageCoding = (languageComponent.Value as CodeableConcept)?.Coding?.FirstOrDefault();
                PreferredLanguage = languageCoding?.Display ?? languageCoding?.Code;
            }

            if (interpreterComponent != null && interpreterComponent.Value is FhirBoolean interpreterBool && interpreterBool.Value.HasValue)
            {
                IsInterpreterRequired = interpreterBool.Value.Value.ToString();
            }
        }

        // Reason for Removal and effective date
        var removalExtension = patient.Extension?.FirstOrDefault(e =>
            e.Url == "https://fhir.nhs.uk/StructureDefinition/Extension-PDS-RemovalFromRegistration");
        if (removalExtension != null)
        {
            var removalCodeExtension = removalExtension.Extension?.FirstOrDefault(e => e.Url == "removalFromRegistrationCode");
            var effectiveTimeExtension = removalExtension.Extension?.FirstOrDefault(e => e.Url == "effectiveTime");

            if (removalCodeExtension?.Value is CodeableConcept removalCodeable)
            {
                var removalCoding = removalCodeable.Coding?.FirstOrDefault();
                //TODO: don't know where to store this, no field in demographic model but mentioned in confluence
            }

            if (effectiveTimeExtension?.Value is Period effectivePeriod)
            {
                // TODO: same as above - don't know where to store
            }
        }
    }
}

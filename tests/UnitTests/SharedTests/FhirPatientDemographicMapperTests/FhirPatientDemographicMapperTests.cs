namespace FhirParserHelperTests;

using Common;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Model;
using Model.Enums;

[TestClass]
public class FhirParserHelperTests
{
    private readonly Mock<ILogger<FhirPatientDemographicMapper>> _logger = new();
    private readonly FhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<FunctionContext> _context = new();

    public FhirParserHelperTests()
    {
        _fhirPatientDemographicMapper = new FhirPatientDemographicMapper(_logger.Object);

        _request = new Mock<HttpRequestData>(_context.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
            {
                var response = new Mock<HttpResponseData>(_context.Object);
                response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
                response.SetupProperty(r => r.StatusCode);
                response.SetupProperty(r => r.Body, new MemoryStream());

                return response.Object;
            });
    }

    private static string LoadTestJson(string filename)
    {
        return LoadTestFile(filename, ".json");
    }

    private static string LoadTestXml(string filename)
    {
        return LoadTestFile(filename, ".xml");
    }

    private static string LoadTestFile(string filename, string extension)
    {
        // Add extension if not already present
        string filenameWithExtension = filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? filename
            : $"{filename}{extension}";

        // Get the directory of the currently executing assembly
        string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? string.Empty;

        // Try the original path first
        string originalPath = Path.Combine(assemblyDirectory, "../../../PatientMocks", filenameWithExtension);
        if (File.Exists(originalPath))
        {
            return File.ReadAllText(originalPath);
        }

        // Try the alternative path
        string alternativePath = Path.Combine(assemblyDirectory, "../../../SharedTests/FhirPatientDemographicMapperTests/PatientMocks", filenameWithExtension);
        if (File.Exists(alternativePath))
        {
            return File.ReadAllText(alternativePath);
        }

        // If neither exists, throw a descriptive exception
        string errorMessage = $"Could not find {extension} file '{filename}' in either:\n" +
                              $" - {Path.GetDirectoryName(originalPath)}\n" +
                              $" - {Path.GetDirectoryName(alternativePath)}";

        throw new FileNotFoundException(errorMessage, filenameWithExtension);
    }

    [TestMethod]
    public void FhirParser_ValidJson_ReturnsDemographic()
    {
        // Arrange
        string json = LoadTestJson("complete-patient");

        var expected = new PdsDemographic()
        {
            // Basic Identifiers
            NhsNumber = "9000000009",
            ParticipantId = null,
            RecordUpdateDateTime = null,
            RecordInsertDateTime = null,
            DateOfBirth = "2010-10-22",

            // Primary Care Provider
            PrimaryCareProvider = "Y12345",
            PrimaryCareProviderEffectiveFromDate = "2020-01-01",

            // Name Information
            NamePrefix = "Mrs",
            FirstName = "Jane",
            OtherGivenNames = null, // Only one given name in the sample
            FamilyName = "Smith",
            PreviousFamilyName = null, // No maiden/old name in the sample

            // Gender
            Gender = Gender.Female,

            // Address Information
            AddressLine1 = "1 Trevelyan Square",
            AddressLine2 = "Boar Lane",
            AddressLine3 = "City Centre",
            AddressLine4 = "Leeds",
            AddressLine5 = "West Yorkshire",
            Postcode = "LS1 6AE",
            PafKey = "12345678",
            UsualAddressEffectiveFromDate = "2020-01-01",

            // Death Information
            DateOfDeath = "2010-10-22T00:00:00+00:00",
            DeathStatus = Status.Formal, // "Formal - death notice received from Registrar of Deaths"

            // Contact Information
            TelephoneNumber = "01632960587", // Home phone
            TelephoneNumberEffectiveFromDate = "2020-01-01",
            MobileNumber = null, // No mobile phone in the sample
            MobileNumberEffectiveFromDate = null,
            EmailAddress = "jane.smith@example.com",
            EmailAddressEffectiveFromDate = "2019-01-01",

            // Language Preferences
            PreferredLanguage = "French",
            IsInterpreterRequired = "True",

            // Removal Information
            ReasonForRemoval = "SCT",
            RemovalEffectiveFromDate = "2020-01-01T00:00:00+00:00"
        };

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsInstanceOfType(result, typeof(PdsDemographic));

        // Check that the ConfidentialityLevel is set to Unrestricted
        Assert.IsTrue(result.ConfidentialityCode == "U");

        // Basic Identifiers
        Assert.AreEqual(expected.NhsNumber, result.NhsNumber);
        Assert.AreEqual(expected.ParticipantId, result.ParticipantId);
        Assert.AreEqual(expected.RecordUpdateDateTime, result.RecordUpdateDateTime);
        Assert.AreEqual(expected.RecordInsertDateTime, result.RecordInsertDateTime);
        Assert.AreEqual(expected.DateOfBirth, result.DateOfBirth);

        // Primary Care Provider
        Assert.AreEqual(expected.PrimaryCareProvider, result.PrimaryCareProvider);
        Assert.AreEqual(expected.PrimaryCareProviderEffectiveFromDate, result.PrimaryCareProviderEffectiveFromDate);

        // Name Information
        Assert.AreEqual(expected.NamePrefix, result.NamePrefix);
        Assert.AreEqual(expected.FirstName, result.FirstName);
        Assert.AreEqual(expected.OtherGivenNames, result.OtherGivenNames);
        Assert.AreEqual(expected.FamilyName, result.FamilyName);
        Assert.AreEqual(expected.PreviousFamilyName, result.PreviousFamilyName);

        // Gender
        Assert.AreEqual(expected.Gender, result.Gender);

        // Address Information
        Assert.AreEqual(expected.AddressLine1, result.AddressLine1);
        Assert.AreEqual(expected.AddressLine2, result.AddressLine2);
        Assert.AreEqual(expected.AddressLine3, result.AddressLine3);
        Assert.AreEqual(expected.AddressLine4, result.AddressLine4);
        Assert.AreEqual(expected.AddressLine5, result.AddressLine5);
        Assert.AreEqual(expected.Postcode, result.Postcode);
        Assert.AreEqual(expected.PafKey, result.PafKey);
        Assert.AreEqual(expected.UsualAddressEffectiveFromDate, result.UsualAddressEffectiveFromDate);

        // Death Information
        Assert.AreEqual(expected.DateOfDeath, result.DateOfDeath);
        Assert.AreEqual(expected.DeathStatus, result.DeathStatus);

        // Contact Information
        Assert.AreEqual(expected.TelephoneNumber, result.TelephoneNumber);
        Assert.AreEqual(expected.TelephoneNumberEffectiveFromDate, result.TelephoneNumberEffectiveFromDate);
        Assert.AreEqual(expected.MobileNumber, result.MobileNumber);
        Assert.AreEqual(expected.MobileNumberEffectiveFromDate, result.MobileNumberEffectiveFromDate);
        Assert.AreEqual(expected.EmailAddress, result.EmailAddress);
        Assert.AreEqual(expected.EmailAddressEffectiveFromDate, result.EmailAddressEffectiveFromDate);

        // Language Preferences
        Assert.AreEqual(expected.PreferredLanguage, result.PreferredLanguage);
        Assert.AreEqual(expected.IsInterpreterRequired, result.IsInterpreterRequired);

        // Removal Information
        Assert.AreEqual(expected.ReasonForRemoval, result.ReasonForRemoval);
        Assert.AreEqual(expected.RemovalEffectiveFromDate, result.RemovalEffectiveFromDate);
    }

    [TestMethod]
    public void FhirParser_SensitivePatient_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-sensitive");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNotNull(result);

        // Check that the ConfidentialityLevel is set to Restricted
        Assert.IsTrue(result.ConfidentialityCode == "R");

        // Basic Information
        Assert.AreEqual("9000000025", result.NhsNumber);
        Assert.AreEqual("2010-10-22", result.DateOfBirth);
        Assert.AreEqual(Gender.Female, result.Gender);

        // Name Information
        Assert.AreEqual("Mrs", result.NamePrefix);
        Assert.AreEqual("Janet", result.FirstName);
        Assert.AreEqual("Smythe", result.FamilyName);

        // Death Information
        Assert.AreEqual("2010-10-22T00:00:00+00:00", result.DateOfDeath);
        Assert.AreEqual(Status.Formal, result.DeathStatus);

        // Missing Information
        Assert.IsNull(result.AddressLine1);
        Assert.IsNull(result.Postcode);
        Assert.IsNull(result.TelephoneNumber);
        Assert.IsNull(result.EmailAddress);
        Assert.IsNull(result.PrimaryCareProvider);
    }

    [TestMethod]
    public void FhirParser_MinimalPatientData_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-minimal-data");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNotNull(result);

        // Check that the ConfidentialityLevel is set to NotSpecified
        Assert.IsTrue(result.ConfidentialityCode == "U");

        // Only NHS Number should be present
        Assert.AreEqual("9000000033", result.NhsNumber);

        // All other fields should be null
        Assert.IsNull(result.DateOfBirth);
        Assert.IsNull(result.Gender);
        Assert.IsNull(result.NamePrefix);
        Assert.IsNull(result.FirstName);
        Assert.IsNull(result.FamilyName);
        Assert.IsNull(result.OtherGivenNames);
        Assert.IsNull(result.PreviousFamilyName);
        Assert.IsNull(result.AddressLine1);
        Assert.IsNull(result.AddressLine2);
        Assert.IsNull(result.AddressLine3);
        Assert.IsNull(result.AddressLine4);
        Assert.IsNull(result.AddressLine5);
        Assert.IsNull(result.Postcode);
        Assert.IsNull(result.PafKey);
        Assert.IsNull(result.UsualAddressEffectiveFromDate);
        Assert.IsNull(result.DateOfDeath);
        Assert.IsNull(result.DeathStatus);
        Assert.IsNull(result.TelephoneNumber);
        Assert.IsNull(result.TelephoneNumberEffectiveFromDate);
        Assert.IsNull(result.MobileNumber);
        Assert.IsNull(result.MobileNumberEffectiveFromDate);
        Assert.IsNull(result.EmailAddress);
        Assert.IsNull(result.EmailAddressEffectiveFromDate);
        Assert.IsNull(result.PreferredLanguage);
        Assert.IsFalse(bool.Parse(result.IsInterpreterRequired!));
        Assert.IsNull(result.PrimaryCareProvider);
        Assert.IsNull(result.PrimaryCareProviderEffectiveFromDate);
        Assert.IsNull(result.ReasonForRemoval);
        Assert.IsNull(result.RemovalEffectiveFromDate);
        Assert.IsNull(result.RemovalEffectiveToDate);
    }

    [TestMethod]
    public void FhirParser_InvalidJson_ThrowsException()
    {
        // Arrange
        var json = string.Empty;

        // Act & Assert
        Assert.ThrowsException<FormatException>(() => _fhirPatientDemographicMapper.ParseFhirJson(json));
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to parse FHIR json")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [TestMethod]
    public void FhirParser_PatientWithMultipleGivenNames_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-multiple-given-names");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.AreEqual("Jane", result.FirstName);
        Assert.AreEqual("Marie Louise", result.OtherGivenNames);
    }

    [TestMethod]
    public void FhirParser_PatientWithPreviousName_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-with-previous-name");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.AreEqual("Smith", result.FamilyName);
        Assert.AreEqual("Johnson", result.PreviousFamilyName);
    }

    [TestMethod]
    public void FhirParser_PatientWithMobileNumber_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-with-mobile-number");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.AreEqual("07700900123", result.MobileNumber);
        Assert.AreEqual("2020-01-01", result.MobileNumberEffectiveFromDate);
    }

    [TestMethod]
    public void FhirParser_PatientWithDifferentGenders_MapsCorrectly()
    {
        // Test for Male
        var maleJson = LoadTestJson("patient-male");
        var maleResult = _fhirPatientDemographicMapper.ParseFhirJson(maleJson);
        Assert.AreEqual(Gender.Male, maleResult.Gender);

        // Test for Other
        var otherJson = LoadTestJson("patient-other");
        var otherResult = _fhirPatientDemographicMapper.ParseFhirJson(otherJson);
        Assert.AreEqual(Gender.NotSpecified, otherResult.Gender);

        // Test for Unknown
        var unknownJson = LoadTestJson("patient-unknown");
        var unknownResult = _fhirPatientDemographicMapper.ParseFhirJson(unknownJson);
        Assert.AreEqual(Gender.NotKnown, unknownResult.Gender);
    }

    [TestMethod]
    public void FhirParser_NonDeceasedPatient_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-non-deceased");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNull(result.DateOfDeath);
        Assert.IsNull(result.DeathStatus);
    }

    [TestMethod]
    public void FhirParser_InformalDeathStatus_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-informal-death");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.AreEqual(Status.Informal, result.DeathStatus);
    }

    [TestMethod]
    public void FhirParser_MissingOptionalFields_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-minimal");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert - Verify all optional fields are null
        Assert.IsNotNull(result);
        Assert.AreEqual("9000000009", result.NhsNumber);
        Assert.IsNull(result.FirstName);
        Assert.IsNull(result.FamilyName);
        Assert.IsNull(result.NamePrefix);
        Assert.IsNull(result.OtherGivenNames);
        Assert.IsNull(result.AddressLine1);
        Assert.IsNull(result.Postcode);
        Assert.IsNull(result.TelephoneNumber);
        Assert.IsNull(result.EmailAddress);
        Assert.IsNull(result.PreferredLanguage);
        Assert.IsNull(result.ReasonForRemoval);
        Assert.IsNull(result.RemovalEffectiveFromDate);
        Assert.IsNull(result.RemovalEffectiveToDate);

        // Check that the ConfidentialityLevel is set to NotSpecified
        Assert.IsTrue(result.ConfidentialityCode == "");
    }

    [TestMethod]
    public void FhirParser_LanguageWithNoInterpreter_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-language-no-interpreter");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.AreEqual("French", result.PreferredLanguage);
        Assert.IsNull(result.IsInterpreterRequired);
    }

    [TestMethod]
    public void FhirParser_InterpreterNoLanguage_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-interpreter-no-language");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNull(result.PreferredLanguage);
        Assert.AreEqual("True", result.IsInterpreterRequired);
    }

    [TestMethod]
    public void FhirParser_NullPatient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _fhirPatientDemographicMapper.MapPatientToPDSDemographic(null));
    }

    [TestMethod]
    public void FhirParser_PatientWithRemovalInformation_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-with-removal-info");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("SCT", result.ReasonForRemoval); // Using code only
        Assert.AreEqual("2020-01-01T00:00:00+00:00", result.RemovalEffectiveFromDate);
        Assert.AreEqual("2021-12-31T00:00:00+00:00", result.RemovalEffectiveToDate);
    }

    [TestMethod]
    public void FhirParser_PatientWithIncompleteRemovalInfo_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-with-incomplete-removal-info");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("SCT", result.ReasonForRemoval); // Using code only
        Assert.IsNull(result.RemovalEffectiveFromDate); // Missing effective date in the sample
        Assert.IsNull(result.RemovalEffectiveToDate); // Missing effective date in the sample
    }

    [TestMethod]
    public void FhirParser_PatientWithNoEndDate_MapsCorrectly()
    {
        // Arrange
        string json = LoadTestJson("patient-with-no-end-date");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("SCT", result.ReasonForRemoval); // Using code only
        Assert.AreEqual("2020-01-01T00:00:00+00:00", result.RemovalEffectiveFromDate);
        Assert.IsNull(result.RemovalEffectiveToDate); // Missing end date but has start date
    }

    [TestMethod]
    public void FhirParser_PatientWithNoCode_HasNullRemovalReason()
    {
        // Arrange
        string json = LoadTestJson("patient-with-display-only");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJson(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result.ReasonForRemoval); // No code available, should be null
        Assert.AreEqual("2020-01-01T00:00:00+00:00", result.RemovalEffectiveFromDate);
        Assert.AreEqual("2021-12-31T00:00:00+00:00", result.RemovalEffectiveToDate);
    }

    [TestMethod]
    public void ParseFhirJsonNhsNumber_ValidJson_ReturnsString()
    {
        // Arrange
        string json = LoadTestJson("complete-patient");
        string expected = "9000000009";

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirJsonNhsNumber(json);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ParseFhirJsonNhsNumber_InvalidJson_ThrowsException()
    {
        // Arrange
        var json = string.Empty;

        // Act & Assert
        Assert.ThrowsException<FormatException>(() => _fhirPatientDemographicMapper.ParseFhirJsonNhsNumber(json));
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to parse FHIR json")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [TestMethod]
    public void ParseFhirXmlNhsNumber_ValidNemsBundle_ReturnsNhsNumber()
    {
        // Arrange
        string xml = LoadTestXml("nems-bundle");
        string expected = "9000000009";

        // Debug: Check if Patient exists in XML
        Assert.IsTrue(xml.Contains("<Patient>"), "XML should contain Patient element");
        Assert.IsTrue(xml.Contains("9000000009"), "XML should contain NHS number");

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirXmlNhsNumber(xml);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ParseFhirXmlNhsNumber_InvalidXml_ThrowsException()
    {
        // Arrange
        var xml = "<invalid>xml</invalid>";

        // Act & Assert
        Assert.ThrowsException<FormatException>(() => _fhirPatientDemographicMapper.ParseFhirXmlNhsNumber(xml));
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to parse FHIR XML")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [TestMethod]
    public void ParseFhirXmlNhsNumber_BundleWithoutPatient_ReturnsEmpty()
    {
        // Arrange
        var xml = @"<Bundle xmlns=""http://hl7.org/fhir"">
                      <id value=""test-bundle""/>
                      <type value=""message""/>
                      <entry>
                        <resource>
                          <MessageHeader>
                            <id value=""test""/>
                          </MessageHeader>
                        </resource>
                      </entry>
                    </Bundle>";

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirXmlNhsNumber(xml);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ParseFhirXmlNhsNumber_PatientXml_ReturnsNhsNumber()
    {
        // Arrange
        var xml = @"<Patient xmlns=""http://hl7.org/fhir"">
                      <id value=""test-patient""/>
                      <identifier>
                        <system value=""https://fhir.nhs.uk/Id/nhs-number""/>
                        <value value=""1234567890""/>
                      </identifier>
                    </Patient>";

        // Act
        var result = _fhirPatientDemographicMapper.ParseFhirXmlNhsNumber(xml);

        // Assert
        Assert.AreEqual("1234567890", result);
    }

    [TestMethod]
    public void Debug_ExtractPatientFromNemsBundle_ShowsPatientXml()
    {
        // Arrange
        string xml = LoadTestXml("nems-bundle");
        
        // Manually extract Patient XML to see what we get
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(xml);
        var nsManager = new System.Xml.XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("fhir", "http://hl7.org/fhir");
        var patientNode = doc.SelectSingleNode("//fhir:Patient", nsManager);
        
        // Debug assertions
        Assert.IsNotNull(patientNode, "Should find Patient node in XML");
        
        var patientXml = patientNode.OuterXml;
        Assert.IsTrue(patientXml.Contains("9000000009"), "Patient XML should contain NHS number");
        
        // Use the same method as the main class to test the full flow
        var result = _fhirPatientDemographicMapper.ParseFhirXmlNhsNumber(xml);
        
        // This will help us debug - if this passes, the issue is elsewhere
        // If this fails, we know the problem is in our parsing logic
        Assert.AreEqual("9000000009", result, "Should extract NHS number from NEMS Bundle");
    }
}

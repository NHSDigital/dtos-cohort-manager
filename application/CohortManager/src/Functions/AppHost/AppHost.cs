var builder = DistributedApplication.CreateBuilder(args);

var containerLifetime = ContainerLifetime.Persistent;

// Database & Migrations
var sqlPassword = builder.AddParameter("SqlPassword", secret: true);
var dbConnectionString = $"Server=localhost,1433;Database=DToSDB;User Id=SA;Password={await sqlPassword.Resource.GetValueAsync(CancellationToken.None)};TrustServerCertificate=True";
var azureSql = builder.AddAzureSqlServer("azuresql")
                        .RunAsContainer(container =>
                        {
                            container.WithEnvironment("ACCEPT_EULA", "Y")
                                .WithPassword(sqlPassword)
                                .WithHostPort(1433)
                                .WithLifetime(containerLifetime);
                        });
var db = azureSql.AddDatabase("DToSDB");
builder.AddProject<Projects.DataServices_Migrations>("db-migrations")
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WaitFor(db);

// Service Bus
const string ServiceBusEmulatorConnectionString = "Endpoint=sb://localhost:7777;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";
builder.AddAzureServiceBus("servicebus").RunAsEmulator(emulator =>
{
    emulator.WithHostPort(7777)
        .WithConfigurationFile("../../../Set-up/service-bus/config.json")
        .WithLifetime(containerLifetime);
});

// Storage
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithBlobPort(10000)
            .WithQueuePort(10001)
            .WithTablePort(10002)
            .WithLifetime(containerLifetime);
    });
storage.AddBlobContainer("inbound");
storage.AddBlobContainer("file-exceptions");
storage.AddBlobContainer("nems-updates");
storage.AddBlobContainer("nems-config");

// WireMock
builder.AddContainer("wiremock", "wiremock/wiremock")
    .WithHttpEndpoint(port: 8080, targetPort: 8080)
    .WithBindMount("../../../Set-up/wiremock/mappings", "/home/wiremock/mappings")
    .WithLifetime(containerLifetime);

// Environment Variables
const string AzureWebJobsStorage = "UseDevelopmentStorage=true";
const string FunctionsWorkerRuntime = "dotnet-isolated";

const string ExceptionFunctionURL = "http://localhost:7070/api/CreateException";
const string StaticValidationURL = "http://localhost:7074/api/StaticValidation";
const string LookupValidationURL = "http://localhost:7075/api/LookupValidation";
const string TransformDataServiceURL = "http://localhost:7080/api/TransformDataService";
const string RemoveOldValidationRecordUrl = "http://localhost:7085/api/RemoveValidationExceptionData";
const string BsSelectOutCodeUrl = "http://localhost:7988/api/BsSelectOutCode";
const string BsSelectGpPracticeUrl = "http://localhost:7988/api/BsSelectGpPractice";
const string CurrentPostingUrl = "http://localhost:7988/api/CurrentPosting";
const string ExcludedSMULookupUrl = "http://localhost:7988/api/ExcludedSMU";
const string LanguageCodeUrl = "http://localhost:7988/api/LanguageCode";
const string ExceptionManagementDataServiceURL = "http://localhost:7911/api/ExceptionManagementDataService";
const string CohortDistributionDataServiceUrl = "http://localhost:7992/api/CohortDistributionDataService";
const string ParticipantDemographicDataServiceURL = "http://localhost:7993/api/ParticipantDemographicDataService";
const string ParticipantManagementURL = "http://localhost:7994/api/ParticipantManagementDataService";
const string RetrievePdsDemographicURL = "http://localhost:8082/api/RetrievePDSDemographic";
const string ManageNemsSubscriptionSubscribeURL = "http://localhost:9081/api/Subscribe";
const string SendServiceNowMessageURL = "http://localhost:9092/api/servicenow/send";

const string CohortDistributionTopic = "cohort-distribution-topic";
const string DistributeParticipantSubscription = "distribute-participant-sub";
const string ParticipantManagementTopic = "participant-management-topic";
const string ServiceNowParticipantManagementTopic = "servicenow-participant-management-topic";
const string ManageServiceNowParticipantSubscription = "manage-servicenow-participant-sub";

const string AcceptableLatencyThresholdMs = "500";

const string NemsFhirEndpoint = "https://msg.intspineservices.nhs.uk/STU3";
const string NemsFromAsid = "200000002527";
const string NemsToAsid = "200000002527";
const string NemsOdsCode = "T8T9T";
var NemsMeshMailboxId = builder.AddParameter("NemsMeshMailboxId", secret: true);
const string NemsLocalCertPath = "./nhs_signed_client.pfx";
var NemsLocalCertPassword = builder.AddParameter("NemsLocalCertPassword", secret: true);
const string IsStubbed = "true";

const string RetrievePdsParticipantURL = "https://sandbox.api.service.nhs.uk/personal-demographics/FHIR/R4/Patient";
const string KId = "RetrievePdsDemographic-DEV1";
const string Audience = "https://int.api.service.nhs.uk/oauth2/token";
const string AuthTokenURL = "https://int.api.service.nhs.uk/oauth2/token";
const string LocalPrivateKeyFileName = "RetrievePdsDemographic-DEV1.pem.key";
const string ClientId = "Get-private-key-from-NHS-dev-portal";
const string UseFakePDSServices = "true";

const string ServiceNowRefreshAccessTokenUrl = "http://localhost:8080/oauth_token.do";
const string ServiceNowUpdateUrl = "http://localhost:8080/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate";
const string ServiceNowResolutionUrl = "http://localhost:8080/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseResolution";
const string ServiceNowGrantType = "refresh_token";
const string ServiceNowClientId = "MockClientId-123";
const string ServiceNowClientSecret = "MockClientSecret-123";
const string ServiceNowRefreshToken = "MockRefreshToken-123";

// CohortDistributionServices
builder.AddProject<Projects.DistributeParticipant>("DistributeParticipant")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ServiceBusConnectionString_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionURL)
    .WithEnvironment("SendServiceNowMessageURL", SendServiceNowMessageURL)
    .WithEnvironment("ParticipantManagementURL", ParticipantManagementURL)
    .WithEnvironment("LookupValidationURL", LookupValidationURL)
    .WithEnvironment("StaticValidationURL", StaticValidationURL)
    .WithEnvironment("CohortDistributionDataServiceUrl", CohortDistributionDataServiceUrl)
    .WithEnvironment("TransformDataServiceURL", TransformDataServiceURL)
    .WithEnvironment("ParticipantDemographicDataServiceURL", ParticipantDemographicDataServiceURL)
    .WithEnvironment("RemoveOldValidationRecordUrl", RemoveOldValidationRecordUrl)
    .WithEnvironment("CohortDistributionTopic", CohortDistributionTopic)
    .WithEnvironment("DistributeParticipantSubscription", DistributeParticipantSubscription);
builder.AddProject<Projects.TransformDataService>("TransformDataService")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionURL)
    .WithEnvironment("CohortDistributionDataServiceUrl", CohortDistributionDataServiceUrl)
    .WithEnvironment("BsSelectOutCodeUrl", BsSelectOutCodeUrl)
    .WithEnvironment("BsSelectGpPracticeUrl", BsSelectGpPracticeUrl)
    .WithEnvironment("CurrentPostingUrl", CurrentPostingUrl)
    .WithEnvironment("ExcludedSMULookupUrl", ExcludedSMULookupUrl)
    .WithEnvironment("LanguageCodeUrl", LanguageCodeUrl)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);

// DemographicServices
builder.AddProject<Projects.ManageNemsSubscription>("ManageNemsSubscription")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionURL)
    .WithEnvironment("NemsFhirEndpoint", NemsFhirEndpoint)
    .WithEnvironment("NemsFromAsid", NemsFromAsid)
    .WithEnvironment("NemsToAsid", NemsToAsid)
    .WithEnvironment("NemsOdsCode", NemsOdsCode)
    .WithEnvironment("NemsMeshMailboxId", NemsMeshMailboxId)
    .WithEnvironment("NemsLocalCertPath", NemsLocalCertPath)
    .WithEnvironment("NemsLocalCertPassword", NemsLocalCertPassword)
    .WithEnvironment("IsStubbed", IsStubbed);
builder.AddProject<Projects.RetrievePDSDemographic>("RetrievePDSDemographic")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ServiceBusConnectionString_client_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceURL)
    .WithEnvironment("ParticipantManagementTopic", ParticipantManagementTopic)
    .WithEnvironment("RetrievePdsParticipantURL", RetrievePdsParticipantURL)
    .WithEnvironment("KId", KId)
    .WithEnvironment("Audience", Audience)
    .WithEnvironment("AuthTokenURL", AuthTokenURL)
    .WithEnvironment("LocalPrivateKeyFileName", LocalPrivateKeyFileName)
    .WithEnvironment("ClientId", ClientId)
    .WithEnvironment("UseFakePDSServices", UseFakePDSServices);

// ExceptionHandling
builder.AddProject<Projects.CreateException>("CreateException")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceURL)
    .WithEnvironment("ExceptionManagementDataServiceURL", ExceptionManagementDataServiceURL);

// ParticipantManagementServices
builder.AddProject<Projects.ManageServiceNowParticipant>("ManageServiceNowParticipant")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ServiceBusConnectionString_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ServiceBusConnectionString_client_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionURL)
    .WithEnvironment("SendServiceNowMessageURL", SendServiceNowMessageURL)
    .WithEnvironment("RetrievePdsDemographicURL", RetrievePdsDemographicURL)
    .WithEnvironment("ParticipantManagementURL", ParticipantManagementURL)
    .WithEnvironment("CohortDistributionTopic", CohortDistributionTopic)
    .WithEnvironment("ManageNemsSubscriptionSubscribeURL", ManageNemsSubscriptionSubscribeURL)
    .WithEnvironment("ServiceNowParticipantManagementTopic", ServiceNowParticipantManagementTopic)
    .WithEnvironment("ManageServiceNowParticipantSubscription", ManageServiceNowParticipantSubscription);

// ScreeningDataServices
builder.AddProject<Projects.CohortDistributionDataService>("CohortDistributionDataService")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ExceptionManagementDataService>("ExceptionManagementDataService")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ParticipantDemographicDataService>("ParticipantDemographicDataService")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ParticipantManagement>("ParticipantManagement")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ReferenceDataService>("ReferenceDataService")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);

// ScreeningValidationService
builder.AddProject<Projects.LookupValidation>("LookupValidation")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionURL)
    .WithEnvironment("BsSelectOutCodeUrl", BsSelectOutCodeUrl)
    .WithEnvironment("BsSelectGpPracticeUrl", BsSelectGpPracticeUrl)
    .WithEnvironment("CurrentPostingUrl", CurrentPostingUrl)
    .WithEnvironment("ExcludedSMULookupUrl", ExcludedSMULookupUrl);
builder.AddProject<Projects.RemoveValidationException>("RemoveValidationException")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionURL)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceURL)
    .WithEnvironment("ExceptionManagementDataServiceURL", ExceptionManagementDataServiceURL);
builder.AddProject<Projects.StaticValidation>("StaticValidation")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionURL);

// ServiceNowIntegration
var backend = builder.AddProject<Projects.ServiceNowMessageHandler>("ServiceNowMessageHandler")
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ServiceBusConnectionString_client_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ServiceNowParticipantManagementTopic", ServiceNowParticipantManagementTopic)
    .WithEnvironment("ServiceNowRefreshAccessTokenUrl", ServiceNowRefreshAccessTokenUrl)
    .WithEnvironment("ServiceNowUpdateUrl", ServiceNowUpdateUrl)
    .WithEnvironment("ServiceNowResolutionUrl", ServiceNowResolutionUrl)
    .WithEnvironment("ServiceNowGrantType", ServiceNowGrantType)
    .WithEnvironment("ServiceNowClientId", ServiceNowClientId)
    .WithEnvironment("ServiceNowClientSecret", ServiceNowClientSecret)
    .WithEnvironment("ServiceNowRefreshToken", ServiceNowRefreshToken);
builder.Build().Run();

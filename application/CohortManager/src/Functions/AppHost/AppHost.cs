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
const string InboundBlobName = "inbound";
const string InboundPoisonBlobName = "inbound-poison";
storage.AddBlobContainer(InboundBlobName);
storage.AddBlobContainer(InboundPoisonBlobName);
storage.AddBlobContainer("nems-updates");
storage.AddBlobContainer("nems-config");

// WireMock
builder.AddContainer("wiremock", "wiremock/wiremock")
    .WithHttpEndpoint(port: 8080, targetPort: 8080)
    .WithBindMount("../../../Set-up/wiremock/mappings", "/home/wiremock/mappings")
    .WithLifetime(containerLifetime);

// Environment Variables
const string FunctionsWorkerRuntime = "dotnet-isolated";
const string AzureWebJobsStorage = "UseDevelopmentStorage=true";

const string ExceptionFunctionUrl = "http://localhost:7070/api/CreateException";
const string StaticValidationUrl = "http://localhost:7074/api/StaticValidation";
const string LookupValidationUrl = "http://localhost:7075/api/LookupValidation";
const string TransformDataServiceUrl = "http://localhost:7080/api/TransformDataService";
const string RemoveOldValidationRecordUrl = "http://localhost:7085/api/RemoveValidationExceptionData";
const string DurableDemographicUrl = "http://localhost:7079/api/DurableDemographicFunction_HttpStart";
const string GetOrchestrationStatusUrl = "http://localhost:7079/api/GetOrchestrationStatus";
const string BsSelectOutCodeUrl = "http://localhost:7988/api/BsSelectOutCode";
const string BsSelectGpPracticeUrl = "http://localhost:7988/api/BsSelectGpPractice";
const string CurrentPostingUrl = "http://localhost:7988/api/CurrentPosting";
const string ExcludedSMULookupUrl = "http://localhost:7988/api/ExcludedSMU";
const string LanguageCodeUrl = "http://localhost:7988/api/LanguageCode";
const string ExceptionManagementDataServiceUrl = "http://localhost:7911/api/ExceptionManagementDataService";
const string BsSelectRequestAuditDataServiceUrl = "http://localhost:7989/api/BsSelectRequestAuditDataService";
const string CohortDistributionDataServiceUrl = "http://localhost:7992/api/CohortDistributionDataService";
const string ParticipantDemographicDataServiceUrl = "http://localhost:7993/api/ParticipantDemographicDataService";
const string ParticipantManagementUrl = "http://localhost:7994/api/ParticipantManagementDataService";
const string RetrievePdsDemographicUrl = "http://localhost:8082/api/RetrievePDSDemographic";
const string ScreeningLkpDataServiceUrl = "http://localhost:8996/api/ScreeningLkpDataService";
const string ManageNemsSubscriptionSubscribeUrl = "http://localhost:9081/api/Subscribe";
const string SendServiceNowMessageUrl = "http://localhost:9092/api/servicenow/send";

const string CohortDistributionTopic = "cohort-distribution-topic";
const string DistributeParticipantSubscription = "distribute-participant-sub";
const string ParticipantManagementTopic = "participant-management-topic";
const string ServiceNowParticipantManagementTopic = "servicenow-participant-management-topic";
const string ManageServiceNowParticipantSubscription = "manage-servicenow-participant-sub";

const string AcceptableLatencyThresholdMs = "500";

const string MeshApiBaseUrl = "https://localhost:8700/messageexchange";
const string BSSMailBox = "X26ABC1";
var MeshPassword = builder.AddParameter("MeshPassword", secret: true);
var MeshSharedKey = builder.AddParameter("MeshSharedKey", secret: true);
const string MeshKeyName = "meshpfx.pfx";
var MeshKeyPassphrase = builder.AddParameter("MeshKeyPassphrase", secret: true);

const string NemsFhirEndpoint = "https://msg.intspineservices.nhs.uk/STU3";
const string NemsFromAsid = "200000002527";
const string NemsToAsid = "200000002527";
const string NemsOdsCode = "T8T9T";
var NemsMeshMailboxId = builder.AddParameter("NemsMeshMailboxId", secret: true);
const string NemsLocalCertPath = "./nhs_signed_client.pfx";
var NemsLocalCertPassword = builder.AddParameter("NemsLocalCertPassword", secret: true);
const string IsStubbed = "true";

var MeshCaasPassword = builder.AddParameter("MeshCaasPassword", secret: true);
var MeshCaasSharedKey = builder.AddParameter("MeshCaasSharedKey", secret: true);
var MeshCaasKeyName = builder.AddParameter("MeshCaasKeyName", secret: true);
var MeshCaasKeyPassword = builder.AddParameter("MeshCaasKeyPassword", secret: true);
var CaasToMailbox = builder.AddParameter("CaasToMailbox", secret: true);
var CaasFromMailbox = builder.AddParameter("CaasFromMailbox", secret: true);

const string RetrievePdsParticipantUrl = "https://sandbox.api.service.nhs.uk/personal-demographics/FHIR/R4/Patient";
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

// CaasIntegration
builder.AddProject<Projects.receiveCaasFile>(nameof(Projects.receiveCaasFile))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("ServiceBusConnectionString_client_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ParticipantManagementTopic", ParticipantManagementTopic)
    .WithEnvironment("caasfolder_STORAGE", AzureWebJobsStorage)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceUrl)
    .WithEnvironment("DemographicURI", DurableDemographicUrl)
    .WithEnvironment("GetOrchestrationStatusURL", GetOrchestrationStatusUrl)
    .WithEnvironment("ScreeningLkpDataServiceURL", ScreeningLkpDataServiceUrl)
    .WithEnvironment("AllowDeleteRecords", "true")
    .WithEnvironment("BatchSize", "3500")
    .WithEnvironment("maxNumberOfChecks", "50")
    .WithEnvironment("inboundBlobName", InboundBlobName)
    .WithEnvironment("fileExceptions", InboundPoisonBlobName);
builder.AddProject<Projects.RetrieveMeshFile>(nameof(Projects.RetrieveMeshFile))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("caasfolder_STORAGE", AzureWebJobsStorage)
    .WithEnvironment("MeshApiBaseUrl", MeshApiBaseUrl)
    .WithEnvironment("BSSMailBox", BSSMailBox)
    .WithEnvironment("MeshPassword", MeshPassword)
    .WithEnvironment("MeshSharedKey", MeshSharedKey)
    .WithEnvironment("MeshKeyName", MeshKeyName)
    .WithEnvironment("MeshKeyPassphrase", MeshKeyPassphrase);

// CohortDistributionServices
builder.AddProject<Projects.DistributeParticipant>(nameof(Projects.DistributeParticipant))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("ServiceBusConnectionString_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("CohortDistributionTopic", CohortDistributionTopic)
    .WithEnvironment("DistributeParticipantSubscription", DistributeParticipantSubscription)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("SendServiceNowMessageURL", SendServiceNowMessageUrl)
    .WithEnvironment("ParticipantManagementURL", ParticipantManagementUrl)
    .WithEnvironment("LookupValidationURL", LookupValidationUrl)
    .WithEnvironment("StaticValidationURL", StaticValidationUrl)
    .WithEnvironment("CohortDistributionDataServiceUrl", CohortDistributionDataServiceUrl)
    .WithEnvironment("TransformDataServiceURL", TransformDataServiceUrl)
    .WithEnvironment("ParticipantDemographicDataServiceURL", ParticipantDemographicDataServiceUrl)
    .WithEnvironment("RemoveOldValidationRecordUrl", RemoveOldValidationRecordUrl);
builder.AddProject<Projects.RetrieveCohortDistribution>(nameof(Projects.RetrieveCohortDistribution))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("CohortDistributionDataServiceURL", CohortDistributionDataServiceUrl)
    .WithEnvironment("BsSelectRequestAuditDataService", BsSelectRequestAuditDataServiceUrl)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.RetrieveCohortRequestAudit>(nameof(Projects.RetrieveCohortRequestAudit))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("CohortDistributionDataServiceURL", CohortDistributionDataServiceUrl)
    .WithEnvironment("BsSelectRequestAuditDataService", BsSelectRequestAuditDataServiceUrl)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.TransformDataService>(nameof(Projects.TransformDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("CohortDistributionDataServiceUrl", CohortDistributionDataServiceUrl)
    .WithEnvironment("BsSelectOutCodeUrl", BsSelectOutCodeUrl)
    .WithEnvironment("BsSelectGpPracticeUrl", BsSelectGpPracticeUrl)
    .WithEnvironment("CurrentPostingUrl", CurrentPostingUrl)
    .WithEnvironment("ExcludedSMULookupUrl", ExcludedSMULookupUrl)
    .WithEnvironment("LanguageCodeUrl", LanguageCodeUrl)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);

// DemographicServices
builder.AddProject<Projects.DemographicDurableFunction>(nameof(Projects.DemographicDurableFunction))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceUrl)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ManageCaasSubscription>(nameof(Projects.ManageCaasSubscription))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("MeshApiBaseUrl", MeshApiBaseUrl)
    .WithEnvironment("MeshCaasPassword", MeshCaasPassword)
    .WithEnvironment("MeshCaasSharedKey", MeshCaasSharedKey)
    .WithEnvironment("MeshCaasKeyName", MeshCaasKeyName)
    .WithEnvironment("MeshCaasKeyPassword", MeshCaasKeyPassword)
    .WithEnvironment("CaasToMailbox", CaasToMailbox)
    .WithEnvironment("CaasFromMailbox", CaasFromMailbox)
    .WithEnvironment("IsStubbed", IsStubbed);
builder.AddProject<Projects.ManageNemsSubscription>(nameof(Projects.ManageNemsSubscription))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("NemsFhirEndpoint", NemsFhirEndpoint)
    .WithEnvironment("NemsFromAsid", NemsFromAsid)
    .WithEnvironment("NemsToAsid", NemsToAsid)
    .WithEnvironment("NemsOdsCode", NemsOdsCode)
    .WithEnvironment("NemsMeshMailboxId", NemsMeshMailboxId)
    .WithEnvironment("NemsLocalCertPath", NemsLocalCertPath)
    .WithEnvironment("NemsLocalCertPassword", NemsLocalCertPassword)
    .WithEnvironment("IsStubbed", IsStubbed);
builder.AddProject<Projects.RetrievePDSDemographic>(nameof(Projects.RetrievePDSDemographic))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ServiceBusConnectionString_client_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ParticipantManagementTopic", ParticipantManagementTopic)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceUrl)
    .WithEnvironment("RetrievePdsParticipantURL", RetrievePdsParticipantUrl)
    .WithEnvironment("KId", KId)
    .WithEnvironment("Audience", Audience)
    .WithEnvironment("AuthTokenURL", AuthTokenURL)
    .WithEnvironment("LocalPrivateKeyFileName", LocalPrivateKeyFileName)
    .WithEnvironment("ClientId", ClientId)
    .WithEnvironment("UseFakePDSServices", UseFakePDSServices);

// ExceptionHandling
builder.AddProject<Projects.CreateException>(nameof(Projects.CreateException))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceUrl)
    .WithEnvironment("ExceptionManagementDataServiceURL", ExceptionManagementDataServiceUrl);
builder.AddProject<Projects.UpdateException>(nameof(Projects.UpdateException))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionManagementDataServiceURL", ExceptionManagementDataServiceUrl);

// ParticipantManagementServices
builder.AddProject<Projects.ManageServiceNowParticipant>(nameof(Projects.ManageServiceNowParticipant))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ServiceBusConnectionString_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ServiceBusConnectionString_client_internal", ServiceBusEmulatorConnectionString)
    .WithEnvironment("ServiceNowParticipantManagementTopic", ServiceNowParticipantManagementTopic)
    .WithEnvironment("ManageServiceNowParticipantSubscription", ManageServiceNowParticipantSubscription)
    .WithEnvironment("CohortDistributionTopic", CohortDistributionTopic)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("SendServiceNowMessageURL", SendServiceNowMessageUrl)
    .WithEnvironment("RetrievePdsDemographicURL", RetrievePdsDemographicUrl)
    .WithEnvironment("ParticipantManagementURL", ParticipantManagementUrl)
    .WithEnvironment("ManageNemsSubscriptionSubscribeURL", ManageNemsSubscriptionSubscribeUrl);

// ScreeningDataServices
builder.AddProject<Projects.BsSelectRequestAuditDataService>(nameof(Projects.BsSelectRequestAuditDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.CohortDistributionDataService>(nameof(Projects.CohortDistributionDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ExceptionManagementDataService>(nameof(Projects.ExceptionManagementDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.GeneCodeLkpDataService>(nameof(Projects.GeneCodeLkpDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.GetValidationExceptions>(nameof(Projects.GetValidationExceptions))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("ExceptionManagementDataServiceURL", ExceptionManagementDataServiceUrl)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceUrl);
builder.AddProject<Projects.HigherRiskReferralReasonLkpDataService>(nameof(Projects.HigherRiskReferralReasonLkpDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.NemsSubscriptionDataService>(nameof(Projects.NemsSubscriptionDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ParticipantDemographicDataService>(nameof(Projects.ParticipantDemographicDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ParticipantManagement>(nameof(Projects.ParticipantManagement))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ReferenceDataService>(nameof(Projects.ReferenceDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ScreeningLkpDataService>(nameof(Projects.ScreeningLkpDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);
builder.AddProject<Projects.ServiceNowCasesDataService>(nameof(Projects.ServiceNowCasesDataService))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("AcceptableLatencyThresholdMs", AcceptableLatencyThresholdMs);

// ScreeningValidationService
builder.AddProject<Projects.LookupValidation>(nameof(Projects.LookupValidation))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("DtOsDatabaseConnectionString", dbConnectionString)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("BsSelectOutCodeUrl", BsSelectOutCodeUrl)
    .WithEnvironment("BsSelectGpPracticeUrl", BsSelectGpPracticeUrl)
    .WithEnvironment("CurrentPostingUrl", CurrentPostingUrl)
    .WithEnvironment("ExcludedSMULookupUrl", ExcludedSMULookupUrl);
builder.AddProject<Projects.RemoveValidationException>(nameof(Projects.RemoveValidationException))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl)
    .WithEnvironment("DemographicDataServiceURL", ParticipantDemographicDataServiceUrl)
    .WithEnvironment("ExceptionManagementDataServiceURL", ExceptionManagementDataServiceUrl);
builder.AddProject<Projects.StaticValidation>(nameof(Projects.StaticValidation))
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", FunctionsWorkerRuntime)
    .WithEnvironment("AzureWebJobsStorage", AzureWebJobsStorage)
    .WithEnvironment("ExceptionFunctionURL", ExceptionFunctionUrl);

// ServiceNowIntegration
builder.AddProject<Projects.ServiceNowMessageHandler>(nameof(Projects.ServiceNowMessageHandler))
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

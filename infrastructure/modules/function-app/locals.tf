locals {
  fnapp_urls = {

    processCaasFile                     = "https://${var.names.function-app}-${lower(var.function_app.ProcessCaasFile.name_suffix)}.azurewebsites.net/api/processCaasFile"
    fileValidation                      = "https://${var.names.function-app}-${lower(var.function_app.FileValidation.name_suffix)}.azurewebsites.net/api/FileValidation"
    addParticipant                      = "https://${var.names.function-app}-${lower(var.function_app.AddParticipant.name_suffix)}.azurewebsites.net/api/addParticipant"
    removeParticipant                   = "https://${var.names.function-app}-${lower(var.function_app.RemoveParticipant.name_suffix)}.azurewebsites.net/api/RemoveParticipant"
    updateParticipant                   = "https://${var.names.function-app}-${lower(var.function_app.UpdateParticipant.name_suffix)}.azurewebsites.net/api/updateParticipant"
    updateParticipantDetails            = "https://${var.names.function-app}-${lower(var.function_app.UpdateParticipantDetails.name_suffix)}.azurewebsites.net/api/updateParticipantDetails"
    demographicDataFunction             = "https://${var.names.function-app}-${lower(var.function_app.DemographicDataManagement.name_suffix)}.azurewebsites.net/api/DemographicDataFunction"
    createParticipant                   = "https://${var.names.function-app}-${lower(var.function_app.CreateParticipant.name_suffix)}.azurewebsites.net/api/CreateParticipant"
    markParticipantAsEligible           = "https://${var.names.function-app}-${lower(var.function_app.MarkParticipantAsEligible.name_suffix)}.azurewebsites.net/api/markParticipantAsEligible"
    staticValidation                    = "https://${var.names.function-app}-${lower(var.function_app.StaticValidation.name_suffix)}.azurewebsites.net/api/StaticValidation"
    markParticipantAsIneligible         = "https://${var.names.function-app}-${lower(var.function_app.MarkParticipantAsIneligible.name_suffix)}.azurewebsites.net/api/markParticipantAsIneligible"
    lookupValidation                    = "https://${var.names.function-app}-${lower(var.function_app.LookupValidation.name_suffix)}.azurewebsites.net/api/LookupValidation"
    createException                     = "https://${var.names.function-app}-${lower(var.function_app.CreateException.name_suffix)}.azurewebsites.net/api/CreateException"
    demographicDataService              = "https://${var.names.function-app}-${lower(var.function_app.DemographicDataService.name_suffix)}.azurewebsites.net/api/DemographicDataService"
    retrieveParticipantData             = "https://${var.names.function-app}-${lower(var.function_app.RetrieveParticipantData.name_suffix)}.azurewebsites.net/api/RetrieveParticipantData"
    allocateServiceProvider             = "https://${var.names.function-app}-${lower(var.function_app.AllocateServiceProvider.name_suffix)}.azurewebsites.net/api/AllocateServiceProvider"
    transformDataService                = "https://${var.names.function-app}-${lower(var.function_app.TransformDataService.name_suffix)}.azurewebsites.net/api/TransformDataService"
    addCohortDistributionData           = "https://${var.names.function-app}-${lower(var.function_app.AddCohortDistributionData.name_suffix)}.azurewebsites.net/api/AddCohortDistributionData"
    removeCohortDistributionData        = "https://${var.names.function-app}-${lower(var.function_app.RemoveCohortDistributionData.name_suffix)}.azurewebsites.net/api/RemoveCohortDistributionData"
    createCohortDistribution            = "https://${var.names.function-app}-${lower(var.function_app.CreateCohortDistribution.name_suffix)}.azurewebsites.net/api/CreateCohortDistribution"
    ValidateCohortDistributionRecord    = "https://${var.names.function-app}-${lower(var.function_app.ValidateCohortDistributionRecord.name_suffix)}.azurewebsites.net/api/ValidateCohortDistributionRecord"
  }
  db_connection_string = "Server=${var.names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.db_name}"

}

locals {

  global_app_settings = {
    DOCKER_ENABLE_CI                    = var.docker_CI_enable
    REMOTE_DEBUGGING_ENABLED            = var.remote_debugging_enabled
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.enable_appsrv_storage
  }

}

locals {
  app_settings = {

    ReceiveCaasFile = {

      caasfolder_STORAGE           = var.caasfolder_STORAGE
      targetFunction               = local.fnapp_urls.processCaasFile
      ExceptionFunctionURL         = local.fnapp_urls.createException
      FileValidationURL            = local.fnapp_urls.fileValidation
      DtOsDatabaseConnectionString = local.db_connection_string
    }

    ProcessCaasFile = {

      PMSAddParticipant    = local.fnapp_urls.addParticipant
      PMSRemoveParticipant = local.fnapp_urls.removeParticipant
      PMSUpdateParticipant = local.fnapp_urls.updateParticipant
      DemographicURI       = local.fnapp_urls.demographicDataFunction
      ExceptionFunctionURL = local.fnapp_urls.createException
      StaticValidationURL  = local.fnapp_urls.staticValidation
    }

    AddParticipant = {

      DSaddParticipant             = local.fnapp_urls.createParticipant
      DSmarkParticipantAsEligible  = local.fnapp_urls.markParticipantAsEligible
      DemographicURIGet            = local.fnapp_urls.demographicDataFunction
      StaticValidationURL          = local.fnapp_urls.staticValidation
      ExceptionFunctionURL         = local.fnapp_urls.createException
      CohortDistributionServiceURL = local.fnapp_urls.createParticipant
    }

    RemoveParticipant = {

      markParticipantAsIneligible = local.fnapp_urls.markParticipantAsIneligible
      RemoveCohortDistributionURL = local.fnapp_urls.removeCohortDistributionData
      DemographicURIGet           = local.fnapp_urls.demographicDataFunction
      ExceptionFunctionURL        = local.fnapp_urls.createCohortDistribution
    }

    UpdateParticipant = {

      UpdateParticipant            = local.fnapp_urls.updateParticipantDetails
      CohortDistributionServiceURL = local.fnapp_urls.createCohortDistribution
      DemographicURIGet            = local.fnapp_urls.demographicDataFunction
      StaticValidationURL          = local.fnapp_urls.staticValidation
      ExceptionFunctionURL         = local.fnapp_urls.createException
    }

    CreateParticipant = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      ExceptionFunctionURL         = local.fnapp_urls.createException
    }

    MarkParticipantAsEligible = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      ExceptionFunctionURL         = local.fnapp_urls.createException
    }

    MarkParticipantAsIneligible = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      ExceptionFunctionURL         = local.fnapp_urls.createException
    }

    UpdateParticipantDetails = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      ExceptionFunctionURL         = local.fnapp_urls.createException
    }

    CreateException = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    GetValidationExceptions = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    DemographicDataService = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    FileValidation = {

      ExceptionFunctionURL         = local.fnapp_urls.createException
      inboundBlobName              = "inbound"
      fileExceptions               = "inbound-poison"
    }

    StaticValidation = {

      ExceptionFunctionURL = local.fnapp_urls.createException
      BlobContainerName    = "config"
    }

    LookupValidation = {

      ExceptionFunctionURL = local.fnapp_urls.createException
      BlobContainerName    = "config"
    }

    DemographicDataManagement = {

      DemographicDataServiceURI = local.fnapp_urls.demographicDataService
    }

    AddCohortDistributionData = {

      DtOsDatabaseConnectionString = local.db_connection_string
      ExceptionFunctionURL         = local.fnapp_urls.createException
    }

    RetrieveCohortDistributionData = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    RemoveCohortDistributionData = {

      DtOsDatabaseConnectionString = local.db_connection_string
      ExceptionFunctionURL         = local.fnapp_urls.createException
    }

    TransformDataService = {

    }

    AllocateServiceProvider = {

      ExceptionFunctionURL         = local.fnapp_urls.createException
      CreateValidationExceptionURL = local.fnapp_urls.lookupValidation
    }

    CreateCohortDistribution = {

      RetrieveParticipantDataURL          = local.fnapp_urls.retrieveParticipantData
      AllocateScreeningProviderURL        = local.fnapp_urls.allocateServiceProvider
      TransformDataServiceURL             = local.fnapp_urls.transformDataService
      AddCohortDistributionURL            = local.fnapp_urls.addCohortDistributionData
      ValidateCohortDistributionRecordURL = local.fnapp_urls.ValidateCohortDistributionRecord
    }

    RetrieveParticipantData = {
      DtOsDatabaseConnectionString = local.db_connection_string
    }

    ValidateCohortDistributionRecord = {
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      DtOsDatabaseConnectionString = local.db_connection_string
    }
  }
}

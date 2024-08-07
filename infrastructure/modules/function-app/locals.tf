locals {
  fnapp_urls = {

    processCaasFile                  = "https://${var.names.function-app}-${lower(var.function_app.ProcessCaasFile.name_suffix)}.azurewebsites.net/api/processCaasFile"
    fileValidation                   = "https://${var.names.function-app}-${lower(var.function_app.FileValidation.name_suffix)}.azurewebsites.net/api/FileValidation"
    addParticipant                   = "https://${var.names.function-app}-${lower(var.function_app.AddParticipant.name_suffix)}.azurewebsites.net/api/addParticipant"
    removeParticipant                = "https://${var.names.function-app}-${lower(var.function_app.RemoveParticipant.name_suffix)}.azurewebsites.net/api/RemoveParticipant"
    updateParticipant                = "https://${var.names.function-app}-${lower(var.function_app.UpdateParticipant.name_suffix)}.azurewebsites.net/api/updateParticipant"
    updateParticipantDetails         = "https://${var.names.function-app}-${lower(var.function_app.UpdateParticipantDetails.name_suffix)}.azurewebsites.net/api/updateParticipantDetails"
    demographicDataFunction          = "https://${var.names.function-app}-${lower(var.function_app.DemographicDataManagement.name_suffix)}.azurewebsites.net/api/DemographicDataFunction"
    createParticipant                = "https://${var.names.function-app}-${lower(var.function_app.CreateParticipant.name_suffix)}.azurewebsites.net/api/CreateParticipant"
    markParticipantAsEligible        = "https://${var.names.function-app}-${lower(var.function_app.MarkParticipantAsEligible.name_suffix)}.azurewebsites.net/api/markParticipantAsEligible"
    staticValidation                 = "https://${var.names.function-app}-${lower(var.function_app.StaticValidation.name_suffix)}.azurewebsites.net/api/StaticValidation"
    markParticipantAsIneligible      = "https://${var.names.function-app}-${lower(var.function_app.MarkParticipantAsIneligible.name_suffix)}.azurewebsites.net/api/markParticipantAsIneligible"
    lookupValidation                 = "https://${var.names.function-app}-${lower(var.function_app.LookupValidation.name_suffix)}.azurewebsites.net/api/LookupValidation"
    createException                  = "https://${var.names.function-app}-${lower(var.function_app.CreateException.name_suffix)}.azurewebsites.net/api/CreateException"
    demographicDataService           = "https://${var.names.function-app}-${lower(var.function_app.DemographicDataService.name_suffix)}.azurewebsites.net/api/DemographicDataService"
    retrieveParticipantData          = "https://${var.names.function-app}-${lower(var.function_app.RetrieveParticipantData.name_suffix)}.azurewebsites.net/api/RetrieveParticipantData"
    allocateServiceProvider          = "https://${var.names.function-app}-${lower(var.function_app.AllocateServiceProvider.name_suffix)}.azurewebsites.net/api/AllocateServiceProvider"
    transformDataService             = "https://${var.names.function-app}-${lower(var.function_app.TransformDataService.name_suffix)}.azurewebsites.net/api/TransformDataService"
    addCohortDistributionData        = "https://${var.names.function-app}-${lower(var.function_app.AddCohortDistributionData.name_suffix)}.azurewebsites.net/api/AddCohortDistributionData"
    removeFromCohortDistributionData = "https://${var.names.function-app}-${lower(var.function_app.RemoveFromCohortDistributionData.name_suffix)}.azurewebsites.net/api/RemoveFromCohortDistributionData"
    createCohortDistribution         = "https://${var.names.function-app}-${lower(var.function_app.CreateCohortDistribution.name_suffix)}.azurewebsites.net/api/CreateCohortDistribution"

  }

  db_connection_string = "Server=${var.names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.db_name}"
}

locals {

  global_app_settings = {
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.enable_appsrv_storage
    DOCKER_ENABLE_CI                    = var.docker_CI_enable
  }

}

locals {
  app_settings = {

    ReceiveCaasFile = {

      caasfolder_STORAGE = var.caasfolder_STORAGE
      targetFunction     = local.fnapp_urls.processCaasFile
      FileValidationURL  = local.fnapp_urls.fileValidation
    }

    ProcessCaasFile = {

      PMSAddParticipant    = local.fnapp_urls.addParticipant
      PMSRemoveParticipant = local.fnapp_urls.removeParticipant
      PMSUpdateParticipant = local.fnapp_urls.updateParticipant
      DemographicURI       = local.fnapp_urls.demographicDataFunction
    }

    AddParticipant = {

      DSaddParticipant            = local.fnapp_urls.createParticipant
      DSmarkParticipantAsEligible = local.fnapp_urls.markParticipantAsEligible
      DemographicURIGet           = local.fnapp_urls.demographicDataFunction
      StaticValidationURL         = local.fnapp_urls.staticValidation
      exceptionFunctionURL        = local.fnapp_urls.createException
    }

    RemoveParticipant = {

      markParticipantAsIneligible     = local.fnapp_urls.markParticipantAsIneligible
      DemographicURI                  = local.fnapp_urls.demographicDataFunction
      removeFromCohortDistributionURL = local.fnapp_urls.removeFromCohortDistributionData
      exceptionFunctionURL            = local.fnapp_urls.createException
    }

    UpdateParticipant = {

      UpdateParticipant            = local.fnapp_urls.updateParticipantDetails
      StaticValidationURL          = local.fnapp_urls.staticValidation
      DemographicURIGet            = local.fnapp_urls.demographicDataFunction
      cohortDistributionServiceURL = local.fnapp_urls.createCohortDistribution
      exceptionFunctionURL         = local.fnapp_urls.createException
    }

    CreateParticipant = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      exceptionFunctionURL         = local.fnapp_urls.createException
    }

    MarkParticipantAsEligible = {

      DtOsDatabaseConnectionString = local.db_connection_string
      exceptionFunctionURL         = local.fnapp_urls.createException
    }

    MarkParticipantAsIneligible = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      exceptionFunctionURL         = local.fnapp_urls.createException
    }

    UpdateParticipantDetails = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
      exceptionFunctionURL         = local.fnapp_urls.createException
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

      exceptionFunctionURL = local.fnapp_urls.createException
      inboundBlobName      = "file-exceptions"
    }

    StaticValidation = {

      exceptionFunctionURL = local.fnapp_urls.createException
    }

    LookupValidation = {

      exceptionFunctionURL = local.fnapp_urls.createException
    }

    DemographicDataManagement = {

      DemographicDataServiceURI = local.fnapp_urls.demographicDataService
    }

    AddCohortDistributionData = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    RetrieveCohortDistributionData = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    RemoveFromCohortDistributionData = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    TransformDataService = {

    }

    AllocateServiceProvider = {

      exceptionFunctionURL = local.fnapp_urls.createException
    }

    CreateCohortDistribution = {

      retrieveParticipantDataURL = local.fnapp_urls.retrieveParticipantData
      allocateServiceProviderURL = local.fnapp_urls.allocateServiceProvider
      transformDataServiceURL    = local.fnapp_urls.transformDataService
      addCohortDistributionURL   = local.fnapp_urls.addCohortDistributionData
    }

    RetrieveParticipantData = {

    }
  }
}

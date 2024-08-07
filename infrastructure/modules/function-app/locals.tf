locals {
  fnapp_urls = {

    processCaasFile             = "https://${var.names.function-app}-${lower(var.function_app.ProcessCaasFile.name_suffix)}.azurewebsites.net/api/processCaasFile"
    fileValidation              = "https://${var.names.function-app}-${lower(var.function_app.FileValidation.name_suffix)}.azurewebsites.net/api/FileValidation"
    addParticipant              = "https://${var.names.function-app}-${lower(var.function_app.AddNewParticipant.name_suffix)}.azurewebsites.net/api/addParticipant"
    removeParticipant           = "https://${var.names.function-app}-${lower(var.function_app.RemoveParticipant.name_suffix)}.azurewebsites.net/api/RemoveParticipant"
    updateParticipant           = "https://${var.names.function-app}-${lower(var.function_app.UpdateParticipant.name_suffix)}.azurewebsites.net/api/updateParticipant"
    demographicDataFunction     = "https://${var.names.function-app}-${lower(var.function_app.DemographicDataManagement.name_suffix)}.azurewebsites.net/api/DemographicDataFunction"
    createParticipant           = "https://${var.names.function-app}-${lower(var.function_app.CreateParticipant.name_suffix)}.azurewebsites.net/api/CreateParticipant"
    markParticipantAsEligible   = "https://${var.names.function-app}-${lower(var.function_app.MarkParticipantEligible.name_suffix)}.azurewebsites.net/api/markParticipantAsEligible"
    staticValidation            = "https://${var.names.function-app}-${lower(var.function_app.StaticValidation.name_suffix)}.azurewebsites.net/api/StaticValidation"
    markParticipantAsIneligible = "https://${var.names.function-app}-${lower(var.function_app.MarkParticipantAsIneligible.name_suffix)}.azurewebsites.net/api/markParticipantAsIneligible"
    updateParticipant           = "https://${var.names.function-app}-${lower(var.function_app.UpdateParticipantDetails.name_suffix)}.azurewebsites.net/api/updateParticipantDetails"
    lookupValidation            = "https://${var.names.function-app}-${lower(var.function_app.LookupValidation.name_suffix)}.azurewebsites.net/api/LookupValidation"
    createValidationException   = "https://${var.names.function-app}-${lower(var.function_app.CreateValidationExceptions.name_suffix)}.azurewebsites.net/api/CreateValidationException"
    demographicDataService      = "https://${var.names.function-app}-${lower(var.function_app.DemographicDataService.name_suffix)}.azurewebsites.net/api/DemographicDataService"

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

    receiveCaasFile = {

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

    AddNewParticipant = {

      DSaddParticipant            = local.fnapp_urls.createParticipant
      DSmarkParticipantAsEligible = local.fnapp_urls.markParticipantAsEligible
      DemographicURIGet           = local.fnapp_urls.demographicDataFunction
      StaticValidationURL         = local.fnapp_urls.staticValidation
    }

    RemoveParticipant = {

      markParticipantAsIneligible = local.fnapp_urls.markParticipantAsIneligible
    }

    UpdateParticipant = {

      UpdateParticipant   = local.fnapp_urls.updateParticipant
      StaticValidationURL = local.fnapp_urls.staticValidation
      DemographicURIGet   = local.fnapp_urls.demographicDataFunction
    }

    CreateParticipant = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
    }

    MarkParticipantEligible = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    MarkParticipantAsIneligible = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
    }

    UpdateParticipantDetails = {

      DtOsDatabaseConnectionString = local.db_connection_string
      LookupValidationURL          = local.fnapp_urls.lookupValidation
    }

    CreateValidationExceptions = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    GetValidationExceptions = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    DemographicDataService = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    FileValidation = {

      CreateValidationExceptionURL = local.fnapp_urls.createValidationException
      inboundBlobName              = "file-exceptions"
    }

    StaticValidation = {

      CreateValidationExceptionURL = local.fnapp_urls.createValidationException
    }

    LookupValidation = {

      CreateValidationExceptionURL = local.fnapp_urls.createValidationException
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

    RemoveCohortDistributionData = {

      DtOsDatabaseConnectionString = local.db_connection_string
    }

    TransformData = {

    }

    AllocateServiceProvider = {

      CreateValidationExceptionURL = local.fnapp_urls.createValidationException
    }

    CreateCohortDistribution = {

    }

    RetrieveParticipantData = {

    }
  }
}

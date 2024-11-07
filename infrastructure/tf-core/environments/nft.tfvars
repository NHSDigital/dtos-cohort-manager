application = "cohman"
environment = "NFT"

features = {
  acr_enabled               = false
  api_management_enabled    = false
  event_grid_enabled        = false
  private_endpoints_enabled = false
}

tags = {
  Project = "Cohort-Manager"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.103.0.0/16"
    subnets           = {}
  }
}

acr = {
  resource_group_key = "cohman"
  sku                = "Premium"
  admin_enabled      = false

  uai_name = "dtos-cohort-manager-acr-push"
}

app_insights = {
  name_suffix        = "cohman"
  resource_group_key = "cohman"
  appinsights_type   = "web"

  audit_resource_group_key = "audit"
}

app_service_plan = {
  sku_name = "P2v3"
  os_type  = "Linux"

  autoscale = {
    memory_percentage = {
      metric = "MemoryPercentage"

      capacity_min = "1"
      capacity_max = "5"
      capacity_def = "1"

      time_grain       = "PT1M"
      statistic        = "Average"
      time_window      = "PT10M"
      time_aggregation = "Average"

      inc_operator        = "GreaterThan"
      inc_threshold       = 70
      inc_scale_direction = "Increase"
      inc_scale_type      = "ChangeCount"
      inc_scale_value     = 1
      inc_scale_cooldown  = "PT5M"

      dec_operator        = "LessThan"
      dec_threshold       = 25
      dec_scale_direction = "Decrease"
      dec_scale_type      = "ChangeCount"
      dec_scale_value     = 1
      dec_scale_cooldown  = "PT5M"
    }
  }
}

function_apps = {
  acr_mi_name = "dtos-cohort-manager-acr-push"
  acr_name    = "acrukscohmandev"
  acr_rg_name = "rg-cohort-manager-dev-uks"

  cont_registry_use_mi = true

  docker_CI_enable  = "true"
  docker_env_tag    = "nft"
  docker_img_prefix = "cohort-manager"

  enable_appsrv_storage    = "false"
  ftps_state               = "Disabled"
  https_only               = true
  remote_debugging_enabled = false
  worker_32bit             = false

  fa_config = {

    ReceiveCaasFile = {
      name_suffix                  = "receive-caas-file"
      function_endpoint_name       = "ReceiveCaasFile"
      db_connection_string         = "DtOsDatabaseConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "targetFunction"
          function_app_key = "ProcessCaasFile"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "FileValidationURL"
          function_app_key = "FileValidation"
        }
      ]
    }

    RetrieveMeshFile = {
      name_suffix                  = "retrieve-mesh-file"
      function_endpoint_name       = "RetrieveMeshFile"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "targetFunction"
          function_app_key = "ProcessCaasFile"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "FileValidationURL"
          function_app_key = "FileValidation"
        }
      ]
    }

    ProcessCaasFile = {
      name_suffix            = "process-caas-file"
      function_endpoint_name = "processCaasFile"
      app_urls = [
        {
          env_var_name     = "PMSAddParticipant"
          function_app_key = "AddParticipant"
        },
        {
          env_var_name     = "PMSRemoveParticipant"
          function_app_key = "RemoveParticipant"
        },
        {
          env_var_name     = "PMSUpdateParticipant"
          function_app_key = "UpdateParticipant"
        },
        {
          env_var_name     = "DemographicURI"
          function_app_key = "DemographicDataManagement"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "StaticValidationURL"
          function_app_key = "StaticValidation"
        }
      ]
    }

    AddParticipant = {
      name_suffix            = "add-participant"
      function_endpoint_name = "addParticipant"
      app_urls = [
        {
          env_var_name     = "DSaddParticipant"
          function_app_key = "CreateParticipant"
        },
        {
          env_var_name     = "DSmarkParticipantAsEligible"
          function_app_key = "MarkParticipantAsEligible"
        },
        {
          env_var_name     = "DemographicURIGet"
          function_app_key = "DemographicDataManagement"
        },
        {
          env_var_name     = "StaticValidationURL"
          function_app_key = "StaticValidation"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "CohortDistributionServiceURL"
          function_app_key = "CreateCohortDistribution"
        }
      ]
    }

    RemoveParticipant = {
      name_suffix            = "remove-participant"
      function_endpoint_name = "RemoveParticipant"
      app_urls = [
        {
          env_var_name     = "markParticipantAsIneligible"
          function_app_key = "MarkParticipantAsIneligible"
        },
        {
          env_var_name     = "RemoveCohortDistributionURL"
          function_app_key = "RemoveCohortDistributionData"
        },
        {
          env_var_name     = "DemographicURIGet"
          function_app_key = "DemographicDataManagement"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    UpdateParticipant = {
      name_suffix            = "update-participant"
      function_endpoint_name = "updateParticipant"
      app_urls = [
        {
          env_var_name     = "UpdateParticipant"
          function_app_key = "UpdateParticipantDetails"
        },
        {
          env_var_name     = "CohortDistributionServiceURL"
          function_app_key = "CreateCohortDistribution"
        },
        {
          env_var_name     = "DemographicURIGet"
          function_app_key = "DemographicDataManagement"
        },
        {
          env_var_name     = "StaticValidationURL"
          function_app_key = "StaticValidation"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "DSmarkParticipantAsEligible"
          function_app_key = "MarkParticipantAsEligible"
        },
        {
          env_var_name     = "markParticipantAsIneligible"
          function_app_key = "MarkParticipantAsIneligible"
        }
      ]
    }

    CreateParticipant = {
      name_suffix            = "create-participant"
      function_endpoint_name = "CreateParticipant"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    MarkParticipantAsEligible = {
      name_suffix            = "mark-participant-as-eligible"
      function_endpoint_name = "markParticipantAsEligible"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    MarkParticipantAsIneligible = {
      name_suffix            = "mark-participant-as-ineligible"
      function_endpoint_name = "markParticipantAsIneligible"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    UpdateParticipantDetails = {
      name_suffix            = "update-participant-details"
      function_endpoint_name = "updateParticipantDetails"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    CreateException = {
      name_suffix            = "create-exception"
      function_endpoint_name = "CreateException"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    GetValidationExceptions = {
      name_suffix            = "get-validation-exceptions"
      function_endpoint_name = "GetValidationExceptions"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    DemographicDataService = {
      name_suffix            = "demographic-data-service"
      function_endpoint_name = "DemographicDataService"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    FileValidation = {
      name_suffix                  = "file-validation"
      function_endpoint_name       = "FileValidation"
      storage_account_env_var_name = "caasfolder_STORAGE"
      storage_containers = [
        {
          env_var_name   = "inboundBlobName"
          container_name = "inbound"
        },
        {
          env_var_name   = "fileExceptions"
          container_name = "inbound-poison"
        }
      ]
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    StaticValidation = {
      name_suffix            = "static-validation"
      function_endpoint_name = "StaticValidation"
      db_connection_string   = "DtOsDatabaseConnectionString"
      storage_containers = [
        {
          env_var_name   = "BlobContainerName"
          container_name = "config"
        }
      ]
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "RemoveOldValidationRecord"
          function_app_key = "RemoveValidationExceptionData"
        }
      ]
    }

    LookupValidation = {
      name_suffix            = "lookup-validation"
      function_endpoint_name = "LookupValidation"
      db_connection_string   = "DtOsDatabaseConnectionString"
      storage_containers = [
        {
          env_var_name   = "BlobContainerName"
          container_name = "config"
        }
      ]
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    DemographicDataManagement = {
      name_suffix            = "demographic-data-management"
      function_endpoint_name = "DemographicDataFunction"
      app_urls = [
        {
          env_var_name     = "DemographicDataServiceURI"
          function_app_key = "DemographicDataService"
        }
      ]
    }

    AddCohortDistributionData = {
      name_suffix            = "add-cohort-distribution-data"
      function_endpoint_name = "AddCohortDistributionData"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    RetrieveCohortDistributionData = {
      name_suffix            = "retrieve-cohort-distribution-data"
      function_endpoint_name = "RetrieveCohortDistributionData"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    RemoveCohortDistributionData = {
      name_suffix            = "remove-cohort-distribution-data"
      function_endpoint_name = "RemoveCohortDistributionData"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    TransformDataService = {
      name_suffix            = "transform-data-service"
      function_endpoint_name = "TransformDataService"
      app_urls               = []
    }

    AllocateServiceProvider = {
      name_suffix            = "allocate-service-provider"
      function_endpoint_name = "AllocateServiceProviderToParticipantByService"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "CreateValidationExceptionURL"
          function_app_key = "LookupValidation"
        }
      ]
    }

    CreateCohortDistribution = {
      name_suffix            = "create-cohort-distribution"
      function_endpoint_name = "CreateCohortDistribution"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "RetrieveParticipantDataURL"
          function_app_key = "RetrieveParticipantData"
        },
        {
          env_var_name     = "AllocateScreeningProviderURL"
          function_app_key = "AllocateServiceProvider"
        },
        {
          env_var_name     = "TransformDataServiceURL"
          function_app_key = "TransformDataService"
        },
        {
          env_var_name     = "AddCohortDistributionURL"
          function_app_key = "AddCohortDistributionData"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "ValidateCohortDistributionRecordURL"
          function_app_key = "ValidateCohortDistributionRecord"
        }
      ]
    }

    RetrieveParticipantData = {
      name_suffix            = "retrieve-participant-data"
      function_endpoint_name = "RetrieveParticipantData"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    ValidateCohortDistributionRecord = {
      name_suffix            = "validate-cohort-distribution-record"
      function_endpoint_name = "ValidateCohortDistributionRecord"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        }
      ]
    }

    RetrieveCohortReplay = {
      name_suffix            = "retrieve-cohort-replay"
      function_endpoint_name = "RetrieveCohortReplay"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    RemoveValidationExceptionData = {
      name_suffix            = "remove-validation-exception-data"
      function_endpoint_name = "RemoveValidationExceptionData"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    RetrieveCohortRequestAudit = {
      name_suffix            = "retrieve-cohort-request-audit"
      function_endpoint_name = "RetrieveCohortRequestAudit"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

  }
}

key_vault = {
  #name_suffix = ""
  resource_group_key = "cohman"
  disk_encryption    = true
  soft_del_ret_days  = 7
  purge_prot         = false
  sku_name           = "standard"
}

law = {
  name_suffix        = "cohman"
  resource_group_key = "cohman"

  law_sku        = "PerGB2018"
  retention_days = 30

  audit_resource_group_key = "audit"
}

sqlserver = {
  sql_uai_name       = "dtos-cohort-manager-sql-adm"
  sql_adm_group_name = "sqlsvr_cohman_nft_uks_admin"
  ad_auth_only       = true

  server = {
    resource_group_key            = "cohman"
    sqlversion                    = "12.0"
    tlsversion                    = 1.2
    azure_services_access_enabled = true
  }

  # cohman database
  dbs = {
    cohman = {
      db_name_suffix = "DToSDB"
      collation      = "SQL_Latin1_General_CP1_CI_AS"
      licence_type   = "LicenseIncluded"
      max_gb         = 5
      read_scale     = false
      sku            = "S0"
    }
  }

  fw_rules = {
    passthrough = {
      fw_rule_name = "AllowAccessFromAzure"
      start_ip     = "0.0.0.0"
      end_ip       = "0.0.0.0"
    }
  }
}

storage_accounts = {
  resource_group_key = "cohman"
  sa_config = {
    fnapp = {
      name_suffix                   = "fnappstor"
      account_tier                  = "Standard"
      replication_type              = "LRS"
      public_network_access_enabled = true
    }

    file_exceptions = {
      name_suffix                   = "filexptns"
      account_tier                  = "Standard"
      replication_type              = "LRS"
      public_network_access_enabled = true
    }
  }

  cont_config = {
    file-exceptions = {
      sa_key           = "file_exceptions"
      cont_name        = "file-exceptions"
      cont_access_type = "private"
    }
    config = {
      sa_key           = "file_exceptions"
      cont_name        = "config"
      cont_access_type = "private"
    }
    inbound = {
      sa_key           = "file_exceptions"
      cont_name        = "inbound"
      cont_access_type = "private"
    }

    inbound-poison = {
      sa_key           = "file_exceptions"
      cont_name        = "inbound-poison"
      cont_access_type = "private"
    }
  }
}
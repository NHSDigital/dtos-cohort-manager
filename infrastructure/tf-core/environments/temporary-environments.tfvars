application           = "cohman"
application_full_name = "cohort-manager"
#environment           = "TEMP01"   #This comes from the pipeline

# TODO: merge into the map definition below
rbac_principal_name_key_vault = "dtos_team_select_temp_environments"
rbac_principal_name_resource_group = "dtos_team_select_temp_environments"
rbac_principal_name_storage = "dtos_team_select_temp_environments"

# rbac_principals = {
#   key_vault = {
#     principal_display_name = "dtos_team_select_temp_environments",
#     roles = [
#       "Key Vault Certificates Officer",
#       "Key Vault Certificate User",
#       "Key Vault Crypto Officer",
#       "Key Vault Crypto User",
#       "Key Vault Secrets Officer",
#       "Key Vault Secrets User"
#     ]
#   }
#   resource_group = {
#     principal_display_name = "dtos_team_select_temp_environments",
#     roles = [
#       "Contributor"
#     ]
#   }
#   storage = {
#     principal_display_name = "dtos_team_select_temp_environments",
#     roles = [
#       "Storage Account Contributor",
#       "Storage Blob Data Owner",
#       "Storage Queue Data Contributor"
#     ]
#   }
# }

features = {
  acr_enabled                          = false
  api_management_enabled               = false
  event_grid_enabled                   = false
  private_endpoints_enabled            = false
  private_service_connection_is_manual = false
  public_network_access_enabled        = true
}

tags = {
  Project = "Cohort-Manager"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.254.0.0/16"
    connect_peering   = false
    subnets           = {}
  }
}

acr = {
  admin_enabled = false
  sku           = "Standard"
  uai_name      = "dtos-cohort-manager-acr-push"
}

app_service_plan = {
  os_type                  = "Linux"
  sku_name                 = "P3v3"
  vnet_integration_enabled = true

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

  instances = {
    DefaultPlan = {}
  }
}

diagnostic_settings = {
  metric_enabled = false
}

function_apps = {
  acr_mi_name = "dtos-cohort-manager-acr-push"
  acr_name    = "acrukshubdevcohman"      # Need to leave dev defaults in to avoid changing the data lookups
  acr_rg_name = "rg-hub-dev-uks-cohman"   # Need to leave dev defaults in to avoid changing the data lookups

  always_on = true

  cont_registry_use_mi = true

  docker_CI_enable  = "true"
  docker_env_tag    = ""
  docker_img_prefix = "cohort-manager"

  enable_appsrv_storage         = "false"
  ftps_state                    = "Disabled"
  https_only                    = true
  remote_debugging_enabled      = false
  storage_uses_managed_identity = null
  worker_32bit                  = false

  fa_config = {
    ReceiveCaasFile = {
      name_suffix                  = "receive-caas-file"
      function_endpoint_name       = "ReceiveCaasFile"
      app_service_plan_key         = "DefaultPlan"
      db_connection_string         = "DtOsDatabaseConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "FileValidationURL"
          function_app_key = "FileValidation"
        },
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
          env_var_name     = "StaticValidationURL"
          function_app_key = "StaticValidation"
        },
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        }
      ]
      env_vars_static = {
        BatchSize                  = "2000"
        AddQueueName               = "add-participant-queue"
        recordThresholdForBatching = "3"
        batchDivisionFactor        = "2"
        CheckTimer                 = "100"
        DemographicURI             = "https://dev-uks-durable-demographic-data-service.azurewebsites.net/api/DurableDemographicFunction_HttpStart/"
        GetOrchestrationStatusURL  = "https://dev-uks-durable-demographic-data-service.azurewebsites.net/api/GetOrchestrationStatus"
      }

    }

    RetrieveMeshFile = {
      name_suffix                  = "retrieve-mesh-file"
      function_endpoint_name       = "RetrieveMeshFile"
      app_service_plan_key         = "DefaultPlan"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
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

    AddParticipant = {
      name_suffix                  = "add-participant"
      function_endpoint_name       = "addParticipant"
      app_service_plan_key         = "DefaultPlan"
      storage_account_env_var_name = "caasfolder_STORAGE"
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

      env_vars_static = {
        CohortQueueName = "cohort-distribution-queue"
        AddQueueName    = "add-participant-queue"
      }

    }

    RemoveParticipant = {
      name_suffix            = "remove-participant"
      function_endpoint_name = "RemoveParticipant"
      app_service_plan_key   = "DefaultPlan"
      app_urls = [
        {
          env_var_name     = "markParticipantAsIneligible"
          function_app_key = "MarkParticipantAsIneligible"
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
      app_service_plan_key   = "DefaultPlan"
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
      env_vars_static = {
        CohortQueueName = "cohort-distribution-queue"
      }
    }

    CreateParticipant = {
      name_suffix            = "create-participant"
      function_endpoint_name = "CreateParticipant"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "ParticipantManagementUrl"
          function_app_key = "ParticipantManagementDataService"
        }
      ]
    }

    MarkParticipantAsEligible = {
      name_suffix            = "mark-participant-as-eligible"
      function_endpoint_name = "markParticipantAsEligible"
      app_service_plan_key   = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    GetValidationExceptions = {
      name_suffix            = "get-validation-exceptions"
      function_endpoint_name = "GetValidationExceptions"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    DemographicDataService = {
      name_suffix            = "demographic-data-service"
      function_endpoint_name = "DemographicDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        }
      ]
    }

    FileValidation = {
      name_suffix                  = "file-validation"
      function_endpoint_name       = "FileValidation"
      app_service_plan_key         = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
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
          env_var_name     = "BsSelectGpPracticeUrl"
          function_app_key = "BsSelectGpPracticeDataService"
        },
        {
          env_var_name     = "BsSelectOutCodeUrl"
          function_app_key = "BsSelectOutcodeDataService"
        },
        {
          env_var_name     = "LanguageCodeUrl"
          function_app_key = "LanguageCodeDataService"
        },
        {
          env_var_name     = "CurrentPostingUrl"
          function_app_key = "CurrentPostingDataService"
        },
        {
          env_var_name     = "ExcludedSMULookupUrl"
          function_app_key = "ExcludedSMUDataService"
        }
      ]
    }

    DemographicDataManagement = {
      name_suffix            = "demographic-data-management"
      function_endpoint_name = "DemographicDataFunction"
      app_service_plan_key   = "DefaultPlan"
      app_urls = [
        {
          env_var_name     = "DemographicDataServiceURI"
          function_app_key = "DemographicDataService"
        },
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    AddCohortDistributionData = {
      name_suffix            = "add-cohort-distribution-data"
      function_endpoint_name = "AddCohortDistributionData"
      app_service_plan_key   = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "BsSelectOutCodeUrl"
          function_app_key = "BsSelectOutcodeDataService"
        }
      ]
    }

    AllocateServiceProvider = {
      name_suffix            = "allocate-service-provider"
      function_endpoint_name = "AllocateServiceProviderToParticipantByService"
      app_service_plan_key   = "DefaultPlan"
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
      name_suffix                  = "create-cohort-distribution"
      function_endpoint_name       = "CreateCohortDistribution"
      app_service_plan_key         = "DefaultPlan"
      storage_account_env_var_name = "caasfolder_STORAGE"
      db_connection_string         = "DtOsDatabaseConnectionString"
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
      env_vars_static = {
        CohortQueueName             = "cohort-distribution-queue"
        CohortQueueNamePoison       = "cohort-distribution-queue-poison"
        IgnoreParticipantExceptions = "true"
        IsExtractedToBSSelect       = "false"
      }
    }

    RetrieveParticipantData = {
      name_suffix            = "retrieve-participant-data"
      function_endpoint_name = "RetrieveParticipantData"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    ValidateCohortDistributionRecord = {
      name_suffix            = "validate-cohort-distribution-record"
      function_endpoint_name = "ValidateCohortDistributionRecord"
      app_service_plan_key   = "DefaultPlan"
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

    RemoveValidationExceptionData = {
      name_suffix            = "remove-validation-exception-data"
      function_endpoint_name = "RemoveValidationExceptionData"
      app_service_plan_key   = "DefaultPlan"
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
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }
    LanguageCodeDataService = {
      name_suffix            = "language-code-data-service"
      function_endpoint_name = "LanguageCodeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    CurrentPostingDataService = {
      name_suffix            = "current-posting-data-service"
      function_endpoint_name = "CurrentPostingDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    BsSelectOutcodeDataService = {
      name_suffix            = "bs-select-outcode-data-service"
      function_endpoint_name = "BsSelectOutcodeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    BsSelectGpPracticeDataService = {
      name_suffix            = "bs-select-gp-practice-data-service"
      function_endpoint_name = "BsSelectGpPracticeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    ExcludedSMUDataService = {
      name_suffix            = "excluded-smu-data-service"
      function_endpoint_name = "ExcludedSMUDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    ParticipantManagementDataService = {
      name_suffix            = "participant-management-data-service"
      function_endpoint_name = "ParticipantManagementDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    ParticipantDemographicDataService = {
      name_suffix            = "participant-demographic-data-service"
      function_endpoint_name = "ParticipantDemographicDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    DurableDemographicFunction = {
      name_suffix            = "durable-demographic-data-service"
      function_endpoint_name = "DurableDemographicFunction"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        }
      ]
    }

  }
}

function_app_slots = []

key_vault = {
  disk_encryption   = true
  soft_del_ret_days = 7
  purge_prot        = false
  sku_name          = "standard"
}

sqlserver = {
  sql_uai_name                         = "dtos-cohort-manager-sql-adm"
  sql_admin_group_name                 = "sqlsvr_cohman_temp_uks_admin"
  ad_auth_only                         = true
  public_network_access_enabled        = true
  auditing_policy_retention_in_days    = 30
  security_alert_policy_retention_days = 30

  server = {
    sqlversion                    = "12.0"
    tlsversion                    = 1.2
    azure_services_access_enabled = true
  }

  # cohman database
  dbs = {
    cohman = {
      db_name_suffix       = "DToSDB"
      collation            = "SQL_Latin1_General_CP1_CI_AS"
      licence_type         = "LicenseIncluded"
      max_gb               = 30
      read_scale           = false
      sku                  = "S1"
      storage_account_type = "Local"
      zone_redundant       = false
    }
  }

  fw_rules = {}
}

storage_accounts = {
  fnapp = {
    name_suffix                             = "fnappstor"
    account_tier                            = "Standard"
    replication_type                        = "LRS"
    public_network_access_enabled           = true
    blob_properties_delete_retention_policy = 7
    blob_properties_versioning_enabled      = false
    containers                              = {}
  }
  file_exceptions = {
    name_suffix                             = "filexptns"
    account_tier                            = "Standard"
    replication_type                        = "LRS"
    public_network_access_enabled           = true
    blob_properties_delete_retention_policy = 7
    blob_properties_versioning_enabled      = false
    containers = {
      file-exceptions = {
        container_name        = "file-exceptions"
        container_access_type = "private"
      }
      config = {
        container_name = "config"
      }
      inbound = {
        container_name = "inbound"
      }
      inbound-poison = {
        container_name = "inbound-poison"
      }
    }
  }
}

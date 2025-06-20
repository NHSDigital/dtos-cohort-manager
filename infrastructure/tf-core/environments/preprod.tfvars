application = "cohman"
environment = "PRE"

features = {
  acr_enabled                          = false
  api_management_enabled               = false
  event_grid_enabled                   = false
  private_endpoints_enabled            = true
  private_service_connection_is_manual = false
  public_network_access_enabled        = false
}

tags = {
  Environment = "pre-production"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.2.0.0/16"
    connect_peering   = true
    subnets = {
      apps = {
        cidr_newbits               = 8
        cidr_offset                = 2
        delegation_name            = "Microsoft.Web/serverFarms"
        service_delegation_name    = "Microsoft.Web/serverFarms"
        service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
      }
      pep = {
        cidr_newbits = 8
        cidr_offset  = 1
      }
      sql = {
        cidr_newbits = 8
        cidr_offset  = 3
      }
      webapps = {
        cidr_newbits               = 8
        cidr_offset                = 4
        delegation_name            = "Microsoft.Web/serverFarms"
        service_delegation_name    = "Microsoft.Web/serverFarms"
        service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
      }
      pep-dmz = {
        cidr_newbits = 8
        cidr_offset  = 5
      }
      container-app-db-management = {
        cidr_newbits = 7
        cidr_offset  = 6
      }
    }
  }
}

routes = {
  uksouth = {
    firewall_policy_priority = 100
    application_rules        = []
    nat_rules                = []
    network_rules = [
      {
        name                  = "AllowCohmanToAudit"
        priority              = 800
        action                = "Allow"
        rule_name             = "CohmanToAudit"
        source_addresses      = ["10.2.0.0/16"]
        destination_addresses = ["10.3.0.0/16"]
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      },
      {
        name                  = "AllowAuditToCohman"
        priority              = 810
        action                = "Allow"
        rule_name             = "AuditToCohman"
        source_addresses      = ["10.3.0.0/16"]
        destination_addresses = ["10.2.0.0/16"]
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      }
    ]
    route_table_core = [
      {
        name                   = "EgressViaHubFirewall"
        address_prefix         = "0.0.0.0/0"
        next_hop_type          = "VirtualAppliance"
        next_hop_in_ip_address = "" # will be populated with the Firewall Private IP address
      }
    ]
    route_table_audit = [
      {
        name                   = "AuditToCohman"
        address_prefix         = "10.2.0.0/16"
        next_hop_type          = "VirtualAppliance"
        next_hop_in_ip_address = "" # will be populated with the Firewall Private IP address
      }
    ]
  }
}

app_service_plan = {
  os_type                  = "Linux"
  sku_name                 = "P3v3"
  vnet_integration_enabled = true

  autoscale = {
    scaling_rule = {
      metric = "CpuPercentage"

      capacity_min = "1"
      capacity_max = "12"
      capacity_def = "2"

      time_grain       = "PT1M"
      statistic        = "Average"
      time_window      = "PT1M"
      time_aggregation = "Average"

      inc_operator        = "GreaterThanOrEqual"
      inc_threshold       = 20
      inc_scale_direction = "Increase"
      inc_scale_type      = "ExactCount"
      inc_scale_value     = 12
      inc_scale_cooldown  = "PT10M"

      dec_operator        = "LessThan"
      dec_threshold       = 20
      dec_scale_direction = "Decrease"
      dec_scale_type      = "ExactCount"
      dec_scale_value     = 2
      dec_scale_cooldown  = "PT5M"
    }
  }

  instances = {
    DefaultPlan = {
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_min = "20"
          capacity_max = "20"
          capacity_def = "20"

          inc_threshold   = 5
          dec_threshold   = 5
          inc_scale_value = 20

          dec_scale_type  = "ChangeCount"
          dec_scale_value = 1
        }
      }
    }
    DefaultPlan2 = {
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_min = "5"
          capacity_max = "5"
          capacity_def = "5"

          inc_threshold   = 5
          dec_threshold   = 5
          inc_scale_value = 5

          dec_scale_type  = "ChangeCount"
          dec_scale_value = 1
        }
      }
    }
    HighLoadFunctions = {
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_min = "4"
          capacity_max = "4"
          capacity_def = "4"

          inc_threshold   = 5
          dec_threshold   = 5
          inc_scale_value = 4

          dec_scale_type  = "ChangeCount"
          dec_scale_value = 1
        }
      }
    }
    RetrieveMeshFile = {
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_min = "1"
          capacity_max = "1"
          capacity_def = "1"

          inc_threshold   = 5
          dec_threshold   = 5
          inc_scale_value = 1

          dec_scale_type  = "ChangeCount"
          dec_scale_value = 1
        }
      }
    }
  }
}

container_app_environments = {
  instances = {
    db-management = {
      zone_redundancy_enabled = false
    }
  }
}

container_app_jobs = {
  apps = {
    db-management = {
      container_app_environment_key = "db-management"
      docker_env_tag                = "preprod"
      docker_image                  = "cohort-manager-db-migration"
      container_registry_use_mi     = true
    }
  }
}

diagnostic_settings = {
  metric_enabled = true
}

function_apps = {
  acr_mi_name = "dtos-cohort-manager-acr-push"
  acr_name    = "acrukshubprodcohman"
  acr_rg_name = "rg-hub-prod-uks-cohman"

  app_service_logs_disk_quota_mb         = 35
  app_service_logs_retention_period_days = 7

  always_on = true

  cont_registry_use_mi = true

  docker_CI_enable  = "true"
  docker_env_tag    = "preprod"
  docker_img_prefix = "cohort-manager"

  enable_appsrv_storage         = "false"
  ftps_state                    = "Disabled"
  https_only                    = true
  remote_debugging_enabled      = false
  storage_uses_managed_identity = null
  worker_32bit                  = false
  health_check_path             = "/api/health"

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
          env_var_name     = "PMSAddParticipant"
          function_app_key = "AddParticipant"
        },
        {
          env_var_name     = "PMSRemoveParticipant"
          function_app_key = "RemoveParticipant"
        },
        {
          env_var_name     = "StaticValidationURL"
          function_app_key = "StaticValidation"
        },
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        },
        {
          env_var_name     = "ScreeningLkpDataServiceURL"
          function_app_key = "ScreeningLkpDataService"
        }
      ]
      env_vars_static = {
        BatchSize                  = "2000"
        AddQueueName               = "add-participant-queue"
        recordThresholdForBatching = "3"
        batchDivisionFactor        = "2"
        CheckTimer                 = "100"
        DemographicURI             = "https://pre-uks-durable-demographic-function.azurewebsites.net/api/DurableDemographicFunction_HttpStart/"
        GetOrchestrationStatusURL  = "https://pre-uks-durable-demographic-function.azurewebsites.net/api/GetOrchestrationStatus"
        AllowDeleteRecords         = false
        UpdateQueueName            = "update-participant-queue"
        UseNewFunctions            = "false"
      }

    }

    RetrieveMeshFile = {
      name_suffix                  = "retrieve-mesh-file"
      function_endpoint_name       = "RetrieveMeshFile"
      app_service_plan_key         = "RetrieveMeshFile"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        BypassServerCertificateValidation = "true"
        MeshCertName                      = "MeshCert"
      }
    }

    ProcessNemsUpdate = {
      name_suffix                  = "process-nems-update"
      function_endpoint_name       = "ProcessNemsUpdate"
      app_service_plan_key         = "DefaultPlan"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "RetrievePdsDemographicURL"
          function_app_key = "RetrievePDSDemographic"
        }
      ],
      storage_containers = [
        {
          env_var_name   = "NemsMessages"
          container_name = "nems-messages"
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
          env_var_name     = "UpdateParticipant"
          function_app_key = "UpdateParticipantDetails"
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
        }
      ]
      env_vars_static = {
        CohortQueueName = "cohort-distribution-queue"
        UpdateQueueName = "update-participant-queue"
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
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    update-blocked-flag = {
      name_suffix            = "update-blocked-flag"
      function_endpoint_name = "UpdateBlockedFlag"
      app_service_plan_key   = "DefaultPlan"
      app_urls = [
        {
          env_var_name     = "ParticipantDemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
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

    DeleteParticipant = {
      name_suffix            = "delete-participant"
      function_endpoint_name = "DeleteParticipant"
      app_service_plan_key   = "DefaultPlan"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "CohortDistributionDataServiceURL"
          function_app_key = "CohortDistributionDataService"
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
        },
        {
          env_var_name     = "ParticipantManagementUrl"
          function_app_key = "ParticipantManagementDataService"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    CreateException = {
      name_suffix            = "create-exception"
      function_endpoint_name = "CreateException"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        },
        {
          env_var_name     = "ExceptionManagementDataServiceURL"
          function_app_key = "ExceptionManagementDataService"
        },
        {
          env_var_name     = "GPPracticeDataServiceURL"
          function_app_key = "GPPracticeDataService"
        }
      ]
    }

    GetValidationExceptions = {
      name_suffix            = "get-validation-exceptions"
      function_endpoint_name = "GetValidationExceptions"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        },
        {
          env_var_name     = "ExceptionManagementDataServiceURL"
          function_app_key = "ExceptionManagementDataService"
        },
        {
          env_var_name     = "GPPracticeDataServiceURL"
          function_app_key = "GPPracticeDataService"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
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
          env_var_name     = "ParticipantDemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        },
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
        },
        {
          env_var_name     = "CohortDistributionDataServiceURL"
          function_app_key = "CohortDistributionDataService"
        },
        {
          env_var_name     = "BsSelectRequestAuditDataService"
          function_app_key = "BsSelectRequestAuditDataService"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
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
        },
        {
          env_var_name     = "BsSelectGpPracticeUrl"
          function_app_key = "BsSelectGpPracticeDataService"
        },
        {
          env_var_name     = "LanguageCodeUrl"
          function_app_key = "LanguageCodeDataService"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
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
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        },
        {
          env_var_name     = "ParticipantManagementUrl"
          function_app_key = "ParticipantManagementDataService"
        },
        {
          env_var_name     = "CohortDistributionDataServiceURL"
          function_app_key = "CohortDistributionDataService"
        },
      ]
      env_vars_static = {
        CohortQueueName              = "cohort-distribution-queue"
        CohortQueueNamePoison        = "cohort-distribution-queue-poison"
        IgnoreParticipantExceptions  = "false"
        IsExtractedToBSSelect        = "false"
        AcceptableLatencyThresholdMs = "500"
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
        },
        {
          env_var_name     = "ParticipantManagementUrl"
          function_app_key = "ParticipantManagementDataService"
        },
        {
          env_var_name     = "DemographicDataFunctionURL"
          function_app_key = "DemographicDataManagement"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    RemoveValidationExceptionData = {
      name_suffix            = "remove-validation-exception-data"
      function_endpoint_name = "RemoveValidationExceptionData"
      app_service_plan_key   = "DefaultPlan2"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        },
        {
          env_var_name     = "ExceptionManagementDataServiceURL"
          function_app_key = "ExceptionManagementDataService"
        },
        {
          env_var_name     = "GPPracticeDataServiceURL"
          function_app_key = "GPPracticeDataService"
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
        },
        {
          env_var_name     = "CohortDistributionDataServiceURL"
          function_app_key = "CohortDistributionDataService"
        },
        {
          env_var_name     = "BsSelectRequestAuditDataService"
          function_app_key = "BsSelectRequestAuditDataService"
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
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
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
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
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
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
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
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
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
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    ParticipantManagementDataService = {
      name_suffix            = "participant-management-data-service"
      function_endpoint_name = "ParticipantManagementDataService"
      app_service_plan_key   = "HighLoadFunctions"
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
      app_service_plan_key   = "HighLoadFunctions"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    DurableDemographicFunction = {
      name_suffix            = "durable-demographic-function"
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
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    GPPracticeDataService = {
      name_suffix            = "gppractice-data-service"
      function_endpoint_name = "GPPracticeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    ExceptionManagementDataService = {
      name_suffix            = "exception-management-data-service"
      function_endpoint_name = "ExceptionManagementDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    GeneCodeLkpDataService = {
      name_suffix            = "gene-code-lkp-data-service"
      function_endpoint_name = "GeneCodeLkpDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    HigherRiskReferralReasonLkpDataService = {
      name_suffix            = "higher-risk-referral-reason-lkp-data-service"
      function_endpoint_name = "HigherRiskReferralReasonLkpDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    CohortDistributionDataService = {
      name_suffix            = "cohort-distribution-data-service"
      function_endpoint_name = "CohortDistributionDataService"
      app_service_plan_key   = "HighLoadFunctions"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    ReceiveServiceNowMessage = {
      name_suffix            = "receive-service-now-message"
      function_endpoint_name = "ReceiveServiceNowMessage"
      app_service_plan_key   = "DefaultPlan"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    BsSelectRequestAuditDataService = {
      name_suffix            = "bs-request-audit-data-service"
      function_endpoint_name = "BsSelectRequestAuditDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    ScreeningLkpDataService = {
      name_suffix            = "screening-lkp-data-service"
      function_endpoint_name = "ScreeningLkpDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    RetrievePDSDemographic = {
      name_suffix            = "retrieve-pds-demographic"
      function_endpoint_name = "RetrievePDSDemographic"
      app_service_plan_key   = "DefaultPlan"
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
      env_vars_static = {
        RetrievePdsParticipantURL = ""
      }
    }

    ManageNemsSubscription = {
      name_suffix            = "manage-nems-subscription"
      function_endpoint_name = "ManageNemsSubscription"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }
    ReferenceDataService = {
      name_suffix            = "reference-data-service"
      function_endpoint_name = "ReferenceDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    NemsSubscribe = {
      name_suffix            = "nems-subscribe"
      function_endpoint_name = "NemsSubscribe"
      app_service_plan_key   = "DefaultPlan"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "ParticipantDemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        },
        {
          env_var_name     = "RetrievePdsDemographicURL"
          function_app_key = "RetrievePDSDemographic"
        }
      ]
      env_vars_static = {
        NemsFhirEndpoint = "https://example.com"
      }
    }

    NemsMeshRetrieval = {
      name_suffix                  = "nems-mesh-retrieval"
      function_endpoint_name       = "NemsMeshRetrieval"
      app_service_plan_key         = "RetrieveMeshFile"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "nemsmeshfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        MeshCertName = "MeshCert"
      }
    }

    UpdateException = {
      name_suffix            = "update-exception"
      function_endpoint_name = "UpdateException"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionManagementDataServiceURL"
          function_app_key = "ExceptionManagementDataService"
        }
      ]
    }
  }
}

function_app_slots = []

linux_web_app = {
  acr_mi_name = "dtos-cohort-manager-acr-push"
  acr_name    = "acrukshubprodcohman"
  acr_rg_name = "rg-hub-prod-uks-cohman"

  always_on = true

  cont_registry_use_mi = true

  docker_CI_enable  = "true"
  docker_env_tag    = "integration"
  docker_img_prefix = "cohort-manager"

  enable_appsrv_storage    = "false"
  ftps_state               = "Disabled"
  https_only               = true
  remote_debugging_enabled = false
  worker_32bit             = false
  # storage_name             = "webappstor"
  # storage_type             = "AzureBlob"
  # share_name               = "webapp"

  linux_web_app_config = {

    FrontEndUi = {
      name_suffix          = "web"
      app_service_plan_key = "DefaultPlan"
      env_vars = {
        static = {
          AUTH_CIS2_ISSUER_URL = ""
          AUTH_CIS2_CLIENT_ID  = ""
          AUTH_TRUST_HOST      = "true"
          SERVICE_NAME         = "Cohort Manager"
        }
        from_key_vault = {
          # env_var_name          = "key_vault_secret_name"
          AUTH_CIS2_CLIENT_SECRET = "auth-cis2-client-secret"
          COHORT_MANAGER_USERS    = "cohort-manager-users"
          NEXTAUTH_SECRET         = "nextauth-secret"
        }
        local_urls = {
          # %s becomes the environment and region prefix (e.g. dev-uks)
          EXCEPTIONS_API_URL = "https://%s-get-validation-exceptions.azurewebsites.net"
          NEXTAUTH_URL       = "https://%s-web.azurewebsites.net/api/auth"
        }
      }
    }
  }
}

linux_web_app_slots = []

key_vault = {
  disk_encryption   = true
  soft_del_ret_days = 7
  purge_prot        = true
  sku_name          = "standard"
}

sqlserver = {
  sql_admin_group_name                 = "sqlsvr_cohman_preprod_uks_admin"
  ad_auth_only                         = true
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
      sku                  = "S12"
      storage_account_type = "GeoZone"
      zone_redundant       = false

      short_term_retention_policy = 35
      long_term_retention_policy = {
        weekly_retention  = "P4W"
        monthly_retention = "P12M"
        yearly_retention  = "P10Y"
        week_of_year      = 1
      }
    }
  }

  fw_rules = {}
}

storage_accounts = {
  fnapp = {
    name_suffix                             = "fnappstor"
    account_tier                            = "Standard"
    replication_type                        = "LRS"
    public_network_access_enabled           = false
    blob_properties_delete_retention_policy = 7
    blob_properties_versioning_enabled      = false
    containers                              = {}
  }
  file_exceptions = {
    name_suffix                             = "filexptns"
    account_tier                            = "Standard"
    replication_type                        = "LRS"
    public_network_access_enabled           = false
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

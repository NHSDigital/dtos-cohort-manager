application           = "cohman"
application_full_name = "cohort-manager"
environment           = "NFT"

features = {
  acr_enabled                          = false
  api_management_enabled               = false
  event_grid_enabled                   = false
  private_endpoints_enabled            = true
  private_service_connection_is_manual = false
  public_network_access_enabled        = false
}

# these will be merged with compliance tags in locals.tf
tags = {
  Environment = "non-functional testing"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.103.0.0/16"
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
        cidr_newbits               = 7
        cidr_offset                = 6
        delegation_name            = "Microsoft.App/environments"
        service_delegation_name    = "Microsoft.App/environments"
        service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
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
        priority              = 900
        action                = "Allow"
        rule_name             = "CohmanToAudit"
        source_addresses      = ["10.103.0.0/16"]
        destination_addresses = ["10.104.0.0/16"]
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      },
      {
        name                  = "AllowAuditToCohman"
        priority              = 910
        action                = "Allow"
        rule_name             = "AuditToCohman"
        source_addresses      = ["10.104.0.0/16"]
        destination_addresses = ["10.103.0.0/16"]
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
        address_prefix         = "10.103.0.0/16"
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
      capacity_max = "4"
      capacity_def = "2"

      time_grain       = "PT1M"
      statistic        = "Average"
      time_window      = "PT1M"
      time_aggregation = "Average"

      inc_operator        = "GreaterThanOrEqual"
      inc_threshold       = 20
      inc_scale_direction = "Increase"
      inc_scale_type      = "ExactCount"
      inc_scale_value     = 4
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

          capacity_min = "1"
          capacity_max = "4"
          capacity_def = "2"
        }
      }
    }
    HighLoadFunctions = {
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_min = "1"
          capacity_max = "4"
          capacity_def = "2"

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
      docker_env_tag                = "nft"
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
  acr_name    = "acrukshubdevcohman"
  acr_rg_name = "rg-hub-dev-uks-cohman"

  app_service_logs_disk_quota_mb         = 35
  app_service_logs_retention_period_days = 7

  always_on = true

  cont_registry_use_mi = true

  docker_CI_enable  = "true"
  docker_env_tag    = "nft"
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
      producer_to_service_bus      = ["dtoss-nsp"]
      db_connection_string         = "DtOsDatabaseConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL       = "CreateException"
          PMSAddParticipant          = "AddParticipant"
          PMSRemoveParticipant       = "RemoveParticipant"
          PMSUpdateParticipant       = "UpdateParticipant"
          StaticValidationURL        = "StaticValidation"
          DemographicDataServiceURL  = "ParticipantDemographicDataService"
          ScreeningLkpDataServiceURL = "ScreeningLkpDataService"
        }
        static = {
          BatchSize                  = "2000"
          AddQueueName               = "add-participant-queue"
          recordThresholdForBatching = "3"
          batchDivisionFactor        = "2"
          CheckTimer                 = "100"
          delayBetweenChecks         = "50"
          DemographicURI             = "https://nft-uks-durable-demographic-function.azurewebsites.net/api/DurableDemographicFunction_HttpStart/"
          GetOrchestrationStatusURL  = "https://nft-uks-durable-demographic-function.azurewebsites.net/api/GetOrchestrationStatus"
          maxNumberOfChecks          = "50"
          AllowDeleteRecords         = true
          TopicName                  = "DistributeParticipantQueue"
          UpdateQueueName            = "update-participant-queue"
          maxNumberOfChecks          = "50"
          ServiceBusConnectionString = ""
          UseNewFunctions            = "false"
        }
        storage_containers = {
          inboundBlobName = "inbound"
          fileExceptions  = "inbound-poison"
        }
      }
    }

    RetrieveMeshFile = {
      name_suffix                  = "retrieve-mesh-file"
      function_endpoint_name       = "RetrieveMeshFile"
      app_service_plan_key         = "RetrieveMeshFile"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          MeshCertName = "MeshCert"
        }
      }
    }

    ProcessNemsUpdate = {
      name_suffix                  = "process-nems-update"
      function_endpoint_name       = "ProcessNemsUpdate"
      app_service_plan_key         = "DefaultPlan"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL      = "CreateException"
          RetrievePdsDemographicURL = "RetrievePDSDemographic"
          UnsubscribeNemsSubscriptionUrl = "ManageNemsSubscription"
        }
        static = {
          MeshCertName = "MeshCert"
          UpdateQueueName = "update-participant-queue"
        }
        storage_containers = {
          NemsMessages = "nems-messages"
        }
      }
    }

    AddParticipant = {
      name_suffix                  = "add-participant"
      function_endpoint_name       = "addParticipant"
      app_service_plan_key         = "DefaultPlan"
      storage_account_env_var_name = "caasfolder_STORAGE"
      env_vars = {
        app_urls = {
          DSaddParticipant             = "CreateParticipant"
          DemographicURIGet            = "DemographicDataManagement"
          StaticValidationURL          = "StaticValidation"
          ExceptionFunctionURL         = "CreateException"
          CohortDistributionServiceURL = "CreateCohortDistribution"
        }
        static = {
          CohortQueueName = "cohort-distribution-queue"
          AddQueueName    = "add-participant-queue"
        }
      }
    }

    RemoveParticipant = {
      name_suffix            = "remove-participant"
      function_endpoint_name = "RemoveParticipant"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          UpdateParticipant        = "UpdateParticipantDetails"
          ExceptionFunctionURL     = "CreateException"
          ParticipantManagementUrl = "ParticipantManagementDataService"
        }
        static = {
          CohortQueueName = "cohort-distribution-queue"
        }
      }
    }

    UpdateParticipant = {
      name_suffix            = "update-participant"
      function_endpoint_name = "updateParticipant"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          UpdateParticipant            = "UpdateParticipantDetails"
          CohortDistributionServiceURL = "CreateCohortDistribution"
          DemographicURIGet            = "DemographicDataManagement"
          StaticValidationURL          = "StaticValidation"
          ExceptionFunctionURL         = "CreateException"
        }
        static = {
          CohortQueueName = "cohort-distribution-queue"
          UpdateQueueName = "update-participant-queue"
        }
      }
    }

    CreateParticipant = {
      name_suffix            = "create-participant"
      function_endpoint_name = "CreateParticipant"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          LookupValidationURL      = "LookupValidation"
          ExceptionFunctionURL     = "CreateException"
          ParticipantManagementUrl = "ParticipantManagementDataService"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    update-blocked-flag = {
      name_suffix            = "update-blocked-flag"
      function_endpoint_name = "UpdateBlockedFlag"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ParticipantDemographicDataServiceURL = "ParticipantDemographicDataService"
          ExceptionFunctionURL                 = "CreateException"
          ParticipantManagementUrl             = "ParticipantManagementDataService"
        }
      }
    }

    DeleteParticipant = {
      name_suffix            = "delete-participant"
      function_endpoint_name = "DeleteParticipant"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL             = "CreateException"
          CohortDistributionDataServiceURL = "CohortDistributionDataService"
        }
      }
    }

    UpdateParticipantDetails = {
      name_suffix            = "update-participant-details"
      function_endpoint_name = "updateParticipantDetails"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          LookupValidationURL      = "LookupValidation"
          ExceptionFunctionURL     = "CreateException"
          ParticipantManagementUrl = "ParticipantManagementDataService"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    CreateException = {
      name_suffix            = "create-exception"
      function_endpoint_name = "CreateException"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          DemographicDataServiceURL         = "ParticipantDemographicDataService"
          ExceptionManagementDataServiceURL = "ExceptionManagementDataService"
          GPPracticeDataServiceURL          = "GPPracticeDataService"
        }
      }
    }

    GetValidationExceptions = {
      name_suffix            = "get-validation-exceptions"
      function_endpoint_name = "GetValidationExceptions"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          DemographicDataServiceURL         = "ParticipantDemographicDataService"
          ExceptionManagementDataServiceURL = "ExceptionManagementDataService"
          GPPracticeDataServiceURL          = "GPPracticeDataService"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    StaticValidation = {
      name_suffix            = "static-validation"
      function_endpoint_name = "StaticValidation"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL      = "CreateException"
          RemoveOldValidationRecord = "RemoveValidationExceptionData"
        }
        storage_containers = {
          BlobContainerName = "config"
        }
      }
    }

    LookupValidation = {
      name_suffix            = "lookup-validation"
      function_endpoint_name = "LookupValidation"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL  = "CreateException"
          BsSelectGpPracticeUrl = "BsSelectGpPracticeDataService"
          BsSelectOutCodeUrl    = "BsSelectOutcodeDataService"
          CurrentPostingUrl     = "CurrentPostingDataService"
          ExcludedSMULookupUrl  = "ExcludedSMUDataService"
        }
        storage_containers = {
          BlobContainerName = "config"
        }
      }
    }

    DemographicDataManagement = {
      name_suffix            = "demographic-data-management"
      function_endpoint_name = "DemographicDataFunction"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ParticipantDemographicDataServiceURL = "ParticipantDemographicDataService"
          ExceptionFunctionURL                 = "CreateException"
        }
      }
    }

    RetrieveCohortDistributionData = {
      name_suffix            = "retrieve-cohort-distribution-data"
      function_endpoint_name = "RetrieveCohortDistributionData"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL             = "CreateException"
          CohortDistributionDataServiceURL = "CohortDistributionDataService"
          BsSelectRequestAuditDataService  = "BsSelectRequestAuditDataService"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    TransformDataService = {
      name_suffix            = "transform-data-service"
      function_endpoint_name = "TransformDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL  = "CreateException"
          BsSelectOutCodeUrl    = "BsSelectOutcodeDataService"
          BsSelectGpPracticeUrl = "BsSelectGpPracticeDataService"
          LanguageCodeUrl       = "LanguageCodeDataService"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    AllocateServiceProvider = {
      name_suffix            = "allocate-service-provider"
      function_endpoint_name = "AllocateServiceProviderToParticipantByService"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL         = "CreateException"
          CreateValidationExceptionURL = "LookupValidation"
        }
      }
    }

    CreateCohortDistribution = {
      name_suffix                  = "create-cohort-distribution"
      function_endpoint_name       = "CreateCohortDistribution"
      app_service_plan_key         = "DefaultPlan"
      storage_account_env_var_name = "caasfolder_STORAGE"
      db_connection_string         = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          RetrieveParticipantDataURL       = "RetrieveParticipantData"
          AllocateScreeningProviderURL     = "AllocateServiceProvider"
          TransformDataServiceURL          = "TransformDataService"
          ExceptionFunctionURL             = "CreateException"
          LookupValidationURL              = "LookupValidation"
          ParticipantManagementUrl         = "ParticipantManagementDataService"
          CohortDistributionDataServiceURL = "CohortDistributionDataService"
        }
        static = {
          CohortQueueName              = "cohort-distribution-queue"
          CohortQueueNamePoison        = "cohort-distribution-queue-poison"
          IgnoreParticipantExceptions  = "false"
          IsExtractedToBSSelect        = "false"
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    RetrieveParticipantData = {
      name_suffix            = "retrieve-participant-data"
      function_endpoint_name = "RetrieveParticipantData"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL       = "CreateException"
          ParticipantManagementUrl   = "ParticipantManagementDataService"
          DemographicDataFunctionURL = "DemographicDataManagement"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    RemoveValidationExceptionData = {
      name_suffix            = "remove-validation-exception-data"
      function_endpoint_name = "RemoveValidationExceptionData"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL              = "CreateException"
          DemographicDataServiceURL         = "ParticipantDemographicDataService"
          ExceptionManagementDataServiceURL = "ExceptionManagementDataService"
          GPPracticeDataServiceURL          = "GPPracticeDataService"
        }
      }
    }

    RetrieveCohortRequestAudit = {
      name_suffix            = "retrieve-cohort-request-audit"
      function_endpoint_name = "RetrieveCohortRequestAudit"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL             = "CreateException"
          CohortDistributionDataServiceURL = "CohortDistributionDataService"
          BsSelectRequestAuditDataService  = "BsSelectRequestAuditDataService"
        }
      }
    }

    LanguageCodeDataService = {
      name_suffix            = "language-code-data-service"
      function_endpoint_name = "LanguageCodeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    CurrentPostingDataService = {
      name_suffix            = "current-posting-data-service"
      function_endpoint_name = "CurrentPostingDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    BsSelectOutcodeDataService = {
      name_suffix            = "bs-select-outcode-data-service"
      function_endpoint_name = "BsSelectOutcodeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    BsSelectGpPracticeDataService = {
      name_suffix            = "bs-select-gp-practice-data-service"
      function_endpoint_name = "BsSelectGpPracticeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ExcludedSMUDataService = {
      name_suffix            = "excluded-smu-data-service"
      function_endpoint_name = "ExcludedSMUDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ParticipantManagementDataService = {
      name_suffix            = "participant-management-data-service"
      function_endpoint_name = "ParticipantManagementDataService"
      app_service_plan_key   = "HighLoadFunctions"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
      }
    }

    ParticipantDemographicDataService = {
      name_suffix            = "participant-demographic-data-service"
      function_endpoint_name = "ParticipantDemographicDataService"
      app_service_plan_key   = "HighLoadFunctions"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    DurableDemographicFunction = {
      name_suffix            = "durable-demographic-function"
      function_endpoint_name = "DurableDemographicFunction"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL      = "CreateException"
          DemographicDataServiceURL = "ParticipantDemographicDataService"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    GPPracticeDataService = {
      name_suffix            = "gppractice-data-service"
      function_endpoint_name = "GPPracticeDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ExceptionManagementDataService = {
      name_suffix            = "exception-management-data-service"
      function_endpoint_name = "ExceptionManagementDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    GeneCodeLkpDataService = {
      name_suffix            = "gene-code-lkp-data-service"
      function_endpoint_name = "GeneCodeLkpDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    HigherRiskReferralReasonLkpDataService = {
      name_suffix            = "higher-risk-referral-reason-lkp-data-service"
      function_endpoint_name = "HigherRiskReferralReasonLkpDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    CohortDistributionDataService = {
      name_suffix            = "cohort-distribution-data-service"
      function_endpoint_name = "CohortDistributionDataService"
      app_service_plan_key   = "HighLoadFunctions"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ReceiveServiceNowMessage = {
      name_suffix            = "receive-service-now-message"
      function_endpoint_name = "ReceiveServiceNowMessage"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
      }
    }

    BsSelectRequestAuditDataService = {
      name_suffix            = "bs-request-audit-data-service"
      function_endpoint_name = "BsSelectRequestAuditDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ScreeningLkpDataService = {
      name_suffix            = "screening-lkp-data-service"
      function_endpoint_name = "ScreeningLkpDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ServiceNowCasesDataService = {
      name_suffix            = "servicenow-cases-data-service"
      function_endpoint_name = "ServiceNowCasesDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ServiceNowCohortLookup = {
      name_suffix            = "servicenow-cohort-lookup"
      function_endpoint_name = "ServiceNowCohortLookup"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL             = "CreateException"
          ServiceNowCasesDataServiceURL    = "CohortDistributionDataService"
          CohortDistributionDataServiceURL = "ParticipantDemographicDataService"
        }
      }
    }

    RetrievePDSDemographic = {
      name_suffix            = "retrieve-pds-demographic"
      function_endpoint_name = "RetrievePDSDemographic"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL             = "CreateException"
          DemographicDataServiceURL        = "ParticipantDemographicDataService"
          CohortDistributionDataServiceURL = "ParticipantDemographicDataService"
        }
        static = {
          RetrievePdsParticipantURL = "https://sandbox.api.service.nhs.uk/personal-demographics/FHIR/R4/Patient"
        }
      }
    }

    ManageNemsSubscription = {
      name_suffix            = "manage-nems-subscription"
      function_endpoint_name = "ManageNemsSubscription"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    ReferenceDataService = {
      name_suffix            = "reference-data-service"
      function_endpoint_name = "ReferenceDataService"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          AcceptableLatencyThresholdMs = "500"
        }
      }
    }

    NemsSubscribe = {
      name_suffix            = "nems-subscribe"
      function_endpoint_name = "NemsSubscribe"
      app_service_plan_key   = "DefaultPlan"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL                 = "CreateException"
          ParticipantDemographicDataServiceURL = "ParticipantDemographicDataService"
          RetrievePdsDemographicURL            = "RetrievePDSDemographic"
        }
        static = {
          NemsFhirEndpoint = "https://example.com"
        }
      }
    }

    NemsMeshRetrieval = {
      name_suffix                  = "nems-mesh-retrieval"
      function_endpoint_name       = "NemsMeshRetrieval"
      app_service_plan_key         = "RetrieveMeshFile"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "nemsmeshfolder_STORAGE"
      env_vars = {
        app_urls = {
          ExceptionFunctionURL = "CreateException"
        }
        static = {
          MeshCertName = "MeshCert"
        }
      }
    }

    UpdateException = {
      name_suffix            = "update-exception"
      function_endpoint_name = "UpdateException"
      app_service_plan_key   = "DefaultPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      env_vars = {
        app_urls = {
          ExceptionManagementDataServiceURL = "ExceptionManagementDataService"
        }
      }
    }
  }
}

function_app_slots = []

linux_web_app = {
  acr_mi_name = "dtos-cohort-manager-acr-push"
  acr_name    = "acrukshubdevcohman"
  acr_rg_name = "rg-hub-dev-uks-cohman"

  always_on = true

  cont_registry_use_mi = true

  docker_CI_enable  = "true"
  docker_env_tag    = "nft"
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
          AUTH_CIS2_ISSUER_URL = "https://am.nhsint.auth-ptl.cis2.spineservices.nhs.uk:443"
          AUTH_CIS2_CLIENT_ID  = "5789849932.cohort-manager-ui-dev.b099494b-7c49-4d78-9e3c-3a801aac691b.apps"
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
  purge_prot        = false
  sku_name          = "standard"
}

service_bus = {
  distribute-participant = {
    capacity         = 1
    sku_tier         = "Premium"
    max_payload_size = "100mb"
    topics = {
      cohort-distribution-queue = {
        batched_operations_enabled = true
      }
      add-participant-queue = {
        batched_operations_enabled = true
      }
      update-participant-queue = {
        batched_operations_enabled = true
      }
    }
  }
}

# service_bus_subscriptions = {
#   subscriber_config = {
#     event-dev-ap = {
#       subscription_name       = "events-sub"
#       topic_name              = "events"
#       namespace_name          = "dtoss-nsp"
#       subscriber_functionName = "foundryRelay"
#     }
#   }
# }

sqlserver = {
  sql_admin_group_name                 = "sqlsvr_cohman_nft_uks_admin"
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

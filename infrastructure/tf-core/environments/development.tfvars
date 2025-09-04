application           = "cohman"
application_full_name = "cohort-manager"
environment           = "DEV"

features = {
  acr_enabled                          = false
  api_management_enabled               = false
  event_grid_enabled                   = false
  private_endpoints_enabled            = true
  private_service_connection_is_manual = false
  public_network_access_enabled        = false
  frontdoor_endpoint_enabled           = true
}

# these will be merged with compliance tags in locals.tf
tags = {
  Environment = "development"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.101.0.0/16"
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
        service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/join/action"]
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
        source_addresses      = ["10.101.0.0/16"]
        destination_addresses = ["10.102.0.0/16"]
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      },
      {
        name                  = "AllowAuditToCohman"
        priority              = 910
        action                = "Allow"
        rule_name             = "AuditToCohman"
        source_addresses      = ["10.102.0.0/16"]
        destination_addresses = ["10.101.0.0/16"]
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
        address_prefix         = "10.101.0.0/16"
        next_hop_type          = "VirtualAppliance"
        next_hop_in_ip_address = "" # will be populated with the Firewall Private IP address
      }
    ]
  }
}

app_service_plan = {
  os_type                  = "Linux"
  vnet_integration_enabled = true

  autoscale = {
    scaling_rule = {
      metric = "CpuPercentage"

      capacity_min = "1"
      capacity_max = "3"
      capacity_def = "1"

      time_grain       = "PT1M"
      statistic        = "Average"
      time_window      = "PT1M"
      time_aggregation = "Average"

      inc_operator        = "GreaterThanOrEqual"
      inc_threshold       = 50
      inc_scale_direction = "Increase"
      inc_scale_type      = "ExactCount"
      inc_scale_value     = 3
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

    NonScaling = {
      sku_name = "P2v3"
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_max = "1"
          capacity_def = "1"

          inc_scale_value = 1
          dec_scale_value = 1

        }
      }
    }
    CohortDistributionPlan = {
      sku_name = "P3v3"
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_max    = "5"
          inc_scale_value = 5

          dec_scale_type  = "ChangeCount"
          dec_scale_value = 1
        }
      }
    }
    TarpitPlan = {
      sku_name = "P1v3"
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_max = "1"
          capacity_def = "1"

          inc_scale_value = 1
          dec_scale_value = 1

        }
      }
    }
    ParticipantManagementPlan = {
      sku_name = "P3v3"
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_max    = "5"
          inc_scale_value = 5

          dec_scale_type  = "ChangeCount"
          dec_scale_value = 1
        }
      }
    }
    CohortDistributionOrchPlan = {
      sku_name = "P1v3"
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_max    = "3"
          inc_scale_value = 3

          dec_scale_type  = "ChangeCount"
          dec_scale_value = 1
        }
      }
    }
    ParticipantManagementOrchPlan = {
      sku_name = "P1v3"
      autoscale_override = {
        scaling_rule = {
          metric = "CpuPercentage"

          capacity_max = "3"

          inc_scale_value = 3

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
      app_service_plan_key         = "NonScaling"
      db_connection_string         = "DtOsDatabaseConnectionString"
      service_bus_connections      = ["internal"]
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
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
        },
        {
          env_var_name     = "DemographicURI"
          function_app_key = "DurableDemographicFunction"
          endpoint_name    = "DurableDemographicFunction_HttpStart"
        },
        {
          env_var_name     = "GetOrchestrationStatusURL"
          function_app_key = "DurableDemographicFunction"
          endpoint_name    = "GetOrchestrationStatus"
        }

      ]
      env_vars_static = {
        BatchSize                  = "2000"
        batchDivisionFactor        = "2"
        CheckTimer                 = "100"
        delayBetweenChecks         = "50"
        maxNumberOfChecks          = "50"
        recordThresholdForBatching = "3"
        ParticipantManagementTopic = "participant-management"
        AllowDeleteRecords         = true
      }
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
    }

    RetrieveMeshFile = {
      name_suffix                  = "retrieve-mesh-file"
      function_endpoint_name       = "RetrieveMeshFile"
      app_service_plan_key         = "NonScaling"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "caasfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        MeshCertName                      = "MeshCert"
        BypassServerCertificateValidation = "true"
      }
    }

    ProcessNemsUpdate = {
      name_suffix                  = "process-nems-update"
      function_endpoint_name       = "ProcessNemsUpdate"
      app_service_plan_key         = "NonScaling"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "nemsmeshfolder_STORAGE"
      service_bus_connections      = ["internal"]
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "RetrievePdsDemographicURL"
          function_app_key = "RetrievePDSDemographic"
        },
        {
          env_var_name     = "UnsubscribeNemsSubscriptionUrl"
          function_app_key = "ManageNemsSubscription"
        },
        {
          env_var_name     = "DemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        }
      ],
      storage_containers = [
        {
          env_var_name   = "NemsMessages"
          container_name = "nems-updates"
        },
        {
          env_var_name   = "NemsPoisonContainer"
          container_name = "nems-poison"
        }
      ]
      env_vars_static = {
        MeshCertName               = "MeshCert"
        ParticipantManagementTopic = "participant-management"
      }
    }

    ManageParticipant = {
      name_suffix             = "manage-participant"
      function_endpoint_name  = "ManageParticipant"
      app_service_plan_key    = "ParticipantManagementOrchPlan"
      service_bus_connections = ["internal"]
      app_urls = [
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
        CohortDistributionTopic       = "cohort-distribution"    # Writes to the cohort distribution topic
        ParticipantManagementTopic    = "participant-management" # Subscribes to the participant management topic
        ManageParticipantSubscription = "ManageParticipant"      # Subscribes to the participant management topic
        IgnoreParticipantExceptions   = "false"
        IsExtractedToBSSelect         = "false"
        AcceptableLatencyThresholdMs  = "500"
      }
    }

    ManageServiceNowParticipant = {
      name_suffix             = "manage-servicenow-participant"
      function_endpoint_name  = "ManageServiceNowParticipant"
      app_service_plan_key    = "NonScaling"
      service_bus_connections = ["internal"]
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "RetrievePdsDemographicURL"
          function_app_key = "RetrievePDSDemographic"
        },
        {
          env_var_name     = "SendServiceNowMessageURL"
          function_app_key = "ServiceNowMessageHandler"
          endpoint_name    = "servicenow/send"
        },
        {
          env_var_name     = "ParticipantManagementURL"
          function_app_key = "ParticipantManagementDataService"
        },
        {
          env_var_name     = "ManageNemsSubscriptionSubscribeURL"
          function_app_key = "ManageNemsSubscription"
          endpoint_name    = "Subscribe"
        }
      ]
      env_vars_static = {
        ServiceNowParticipantManagementTopic    = "servicenow-participant-management" # Subscribes to the servicenow participant management topic
        ManageServiceNowParticipantSubscription = "ManageServiceNowParticipant"       # Subscribes to the servicenow participant management topic
        CohortDistributionTopic                 = "cohort-distribution"
      }
    }

    update-blocked-flag = {
      name_suffix            = "update-blocked-flag"
      function_endpoint_name = "UpdateBlockedFlag"
      app_service_plan_key   = "NonScaling"
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
        },
        {
          env_var_name     = "ManageNemsSubscriptionUnsubscribeURL"
          function_app_key = "ManageNemsSubscription"
          endpoint_name    = "Unsubscribe"
        },
        {
          env_var_name     = "ManageNemsSubscriptionSubscribeURL"
          function_app_key = "ManageNemsSubscription"
          endpoint_name    = "Subscribe"
        },
        {
          env_var_name     = "RetrievePdsDemographicURL"
          function_app_key = "RetrievePDSDemographic"
        }
      ]
    }

    DeleteParticipant = {
      name_suffix            = "delete-participant"
      function_endpoint_name = "DeleteParticipant"
      app_service_plan_key   = "ParticipantManagementPlan"
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

    CreateException = {
      name_suffix             = "create-exception"
      function_endpoint_name  = "CreateException"
      app_service_plan_key    = "ParticipantManagementPlan"
      db_connection_string    = "DtOsDatabaseConnectionString"
      service_bus_connections = ["internal"]
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
        CreateExceptionTopic        = "create-exception"
        CreateExceptionSubscription = "CreateException"
      }
    }

    GetValidationExceptions = {
      name_suffix            = "get-validation-exceptions"
      function_endpoint_name = "GetValidationExceptions"
      app_service_plan_key   = "NonScaling"
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
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
      }
    }

    StaticValidation = {
      name_suffix             = "static-validation"
      function_endpoint_name  = "StaticValidation"
      app_service_plan_key    = "CohortDistributionPlan"
      db_connection_string    = "DtOsDatabaseConnectionString"
      service_bus_connections = ["internal"]
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      storage_containers = [
        {
          env_var_name   = "BlobContainerName"
          container_name = "config"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
        CreateExceptionTopic         = "create-exception"
      }
    }

    LookupValidation = {
      name_suffix             = "lookup-validation"
      function_endpoint_name  = "LookupValidation"
      app_service_plan_key    = "CohortDistributionPlan"
      db_connection_string    = "DtOsDatabaseConnectionString"
      service_bus_connections = ["internal"]
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "BsSelectGpPracticeUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "BsSelectGpPractice"
        },
        {
          env_var_name     = "BsSelectOutCodeUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "BsSelectOutCode"
        },
        {
          env_var_name     = "CurrentPostingUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "CurrentPosting"
        },
        {
          env_var_name     = "ExcludedSMULookupUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "ExcludedSMU"
        }
      ]
      storage_containers = [
        {
          env_var_name   = "BlobContainerName"
          container_name = "config"
        }
      ]
    }

    RetrieveCohortDistributionData = {
      name_suffix            = "retrieve-cohort-distribution-data"
      function_endpoint_name = "RetrieveCohortDistributionData"
      app_service_plan_key   = "CohortDistributionPlan"
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
      app_service_plan_key   = "CohortDistributionPlan"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "BsSelectOutCodeUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "BsSelectOutCode"
        },
        {
          env_var_name     = "BsSelectGpPracticeUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "BsSelectGpPractice"
        },
        {
          env_var_name     = "LanguageCodeUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "LanguageCode"
        },
        {
          env_var_name     = "CurrentPostingUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "CurrentPosting"
        },
        {
          env_var_name     = "ExcludedSMULookupUrl"
          function_app_key = "ReferenceDataService"
          endpoint_name    = "ExcludedSMU"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs = "500"
        CacheTimeOutHours            = "24"

      }
    }

    DistributeParticipant = {
      name_suffix             = "distribute-participant"
      function_endpoint_name  = "DistributeParticipant"
      app_service_plan_key    = "CohortDistributionOrchPlan"
      service_bus_connections = ["internal"]
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
          env_var_name     = "participantDemographicDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        },
        {
          env_var_name     = "CohortDistributionDataServiceURL"
          function_app_key = "CohortDistributionDataService"
        },
        {
          env_var_name     = "LookupValidationURL"
          function_app_key = "LookupValidation"
        },
        {
          env_var_name     = "StaticValidationURL"
          function_app_key = "StaticValidation"
        },
        {
          env_var_name     = "TransformDataServiceURL"
          function_app_key = "TransformDataService"
        },
        {
          env_var_name     = "RemoveOldValidationRecordUrl"
          function_app_key = "RemoveValidationExceptionData"
        },
        {
          env_var_name     = "SendServiceNowMessageURL"
          function_app_key = "ServiceNowMessageHandler"
          endpoint_name    = "servicenow/send"
        }
      ]
      env_vars_static = {
        CohortDistributionTopic           = "cohort-distribution"   # Subscribes to the cohort distribution topic
        DistributeParticipantSubscription = "DistributeParticipant" # Subscribes to the cohort distribution topic
        IgnoreParticipantExceptions       = "false"
        IsExtractedToBSSelect             = "false"
        AcceptableLatencyThresholdMs      = "500"
        MaxLookupValidationRetries        = "3"
      }
    }

    RemoveValidationExceptionData = {
      name_suffix            = "remove-validation-exception-data"
      function_endpoint_name = "RemoveValidationExceptionData"
      app_service_plan_key   = "CohortDistributionPlan"
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
      app_service_plan_key   = "NonScaling"
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

    ParticipantManagementDataService = {
      name_suffix            = "participant-management-data-service"
      function_endpoint_name = "ParticipantManagementDataService"
      app_service_plan_key   = "ParticipantManagementPlan"
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
      app_service_plan_key   = "ParticipantManagementPlan"
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
      app_service_plan_key   = "ParticipantManagementPlan"
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
      app_service_plan_key   = "TarpitPlan"
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
      app_service_plan_key   = "CohortDistributionPlan"
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
      app_service_plan_key   = "TarpitPlan"
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
      app_service_plan_key   = "TarpitPlan"
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
      app_service_plan_key   = "CohortDistributionPlan"
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

    ServiceNowMessageHandler = {
      name_suffix             = "servicenow-message-handler"
      function_endpoint_name  = "ServiceNowMessageHandler"
      app_service_plan_key    = "NonScaling"
      key_vault_url           = "KeyVaultConnectionString"
      service_bus_connections = ["internal"]
      env_vars_static = {
        ServiceNowRefreshAccessTokenUrl      = "https://nhsdigitaldev.service-now.com/oauth_token.do"
        ServiceNowUpdateUrl                  = "https://nhsdigitaldev.service-now.com/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate"
        ServiceNowResolutionUrl              = "https://nhsdigitaldev.service-now.com/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseResolution"
        ServiceNowGrantType                  = "refresh_token"
        ServiceNowParticipantManagementTopic = "servicenow-participant-management" # Sends messages to the servicenow participant manage topic
      }
    }

    BsSelectRequestAuditDataService = {
      name_suffix            = "bs-request-audit-data-service"
      function_endpoint_name = "BsSelectRequestAuditDataService"
      app_service_plan_key   = "CohortDistributionPlan"
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
      app_service_plan_key   = "NonScaling"
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

    ServiceNowCasesDataService = {
      name_suffix            = "servicenow-cases-data-service"
      function_endpoint_name = "ServiceNowCasesDataService"
      app_service_plan_key   = "NonScaling"
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

    ServiceNowCohortLookup = {
      name_suffix            = "servicenow-cohort-lookup"
      function_endpoint_name = "ServiceNowCohortLookup"
      app_service_plan_key   = "NonScaling"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        },
        {
          env_var_name     = "ServiceNowCasesDataServiceURL"
          function_app_key = "CohortDistributionDataService"
        },
        {
          env_var_name     = "CohortDistributionDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        }
      ]
    }

    RetrievePDSDemographic = {
      name_suffix             = "retrieve-pds-demographic"
      function_endpoint_name  = "RetrievePDSDemographic"
      app_service_plan_key    = "NonScaling"
      service_bus_connections = ["internal"]
      key_vault_url           = "KeyVaultConnectionString"
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
          env_var_name     = "CohortDistributionDataServiceURL"
          function_app_key = "ParticipantDemographicDataService"
        }
      ]
      env_vars_static = {
        RetrievePdsParticipantURL  = "https://sandbox.api.service.nhs.uk/personal-demographics/FHIR/R4/Patient"
        Kid                        = "RetrievePdsDemographic-DEV1"
        Audience                   = "https://sandbox.api.service.nhs.uk/oauth2/token"
        AuthTokenURL               = "https://sandbox.api.service.nhs.uk/oauth2/token"
        KeyNamePrivateKey          = "PDSPRIVATEKEY"
        UseFakePDSServices         = "true"
        ParticipantManagementTopic = "participant-management"
      }
    }

    ManageNemsSubscription = {
      name_suffix            = "manage-nems-subscription"
      function_endpoint_name = "ManageNemsSubscription"
      app_service_plan_key   = "NonScaling"
      db_connection_string   = "DtOsDatabaseConnectionString"
      key_vault_url          = "KeyVaultConnectionString"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        AcceptableLatencyThresholdMs          = "500"
        NemsFhirEndpoint                      = "https://msg.intspineservices.nhs.uk/STU3"
        NemsFromAsid                          = "200000002527"
        NemsToAsid                            = "200000002527"
        NemsKeyName                           = "nems-client-certificate"
        NemsSubscriptionProfile               = "https://fhir.nhs.uk/STU3/StructureDefinition/EMS-Subscription-1"
        NemsSubscriptionCriteria              = "https://fhir.nhs.uk/Id/nhs-number"
        NemsBypassServerCertificateValidation = "true"
        IsStubbed                             = "true"
      }
    }

    ManageCaasSubscription = {
      name_suffix            = "manage-caas-subscription"
      function_endpoint_name = "ManageCaasSubscription"
      app_service_plan_key   = "NonScaling"
      db_connection_string   = "DtOsDatabaseConnectionString"
      key_vault_url          = "KeyVaultConnectionString"
      env_vars_static = {
        IsStubbed = "true"
      }
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
    }

    ReferenceDataService = {
      name_suffix            = "reference-data-service"
      function_endpoint_name = "ReferenceDataService"
      app_service_plan_key   = "NonScaling"
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
      app_service_plan_key   = "NonScaling"
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
      app_service_plan_key         = "NonScaling"
      key_vault_url                = "KeyVaultConnectionString"
      storage_account_env_var_name = "nemsmeshfolder_STORAGE"
      app_urls = [
        {
          env_var_name     = "ExceptionFunctionURL"
          function_app_key = "CreateException"
        }
      ]
      env_vars_static = {
        NemsMeshCertName                          = "NemsMeshCert"
        NemsMeshInboundContainer                  = "nems-updates"
        NemsMeshConfigContainer                   = "nems-config"
        NemsMeshBypassServerCertificateValidation = "true"
      }
    }

    UpdateException = {
      name_suffix            = "update-exception"
      function_endpoint_name = "UpdateException"
      app_service_plan_key   = "NonScaling"
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
  acr_name    = "acrukshubdevcohman"
  acr_rg_name = "rg-hub-dev-uks-cohman"

  always_on = true

  cont_registry_use_mi = true

  docker_CI_enable  = "true"
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
      app_service_plan_key = "NonScaling"
      env_vars = {
        static = {
          AUTH_CIS2_ISSUER_URL = "https://am.nhsint.auth-ptl.cis2.spineservices.nhs.uk:443"
          AUTH_CIS2_CLIENT_ID  = "0043273051.cohort-dev-non-live.b099494b-7c49-4d78-9e3c-3a801aac691b.apps"
          AUTH_TRUST_HOST      = "true"
          NEXTAUTH_URL         = "https://cohort-dev.non-live.screening.nhs.uk/api/auth"
          SERVICE_NAME         = "Cohort Manager"
          APP_ENV              = "development"
        }
        from_key_vault = {
          # env_var_name           = "key_vault_secret_name"
          AUTH_CIS2_CLIENT_SECRET  = "auth-cis2-client-secret"
          COHORT_MANAGER_RBAC_CODE = "cohort-manager-users"
          NEXTAUTH_SECRET          = "nextauth-secret"
        }
        local_urls = {
          # %s becomes the environment and region prefix (e.g. dev-uks)
          EXCEPTIONS_API_URL = "https://%s-get-validation-exceptions.azurewebsites.net"
        }
      }
    }
  }
}

linux_web_app_slots = []

frontdoor_endpoint = {
  cohort = {
    origin_group = {
      session_affinity_enabled = false
    }
    origin = {
      # Dynamically picks all origins for a specific Web App, adding Private Link connection if enabled (needs manual approval)
      webapp_key = "FrontEndUi" # From var.linux_web_app.linux_web_app_config
    }
    custom_domains = {
      cohort-dev = {
        host_name        = "cohort-dev.non-live.screening.nhs.uk"
        dns_zone_name    = "non-live.screening.nhs.uk"
        dns_zone_rg_name = "rg-hub-dev-uks-public-dns-zones"
      }
    }
    security_policies = {
      AllowedIPs = {
        cdn_frontdoor_firewall_policy_name    = "wafhubnonlivecohmandev"
        cdn_frontdoor_firewall_policy_rg_name = "rg-hub-dev-uks-hub-networking"
        associated_domain_keys                = ["cohort-dev"] # From custom_domains above. Use "endpoint" for the default domain (if linked in Front Door route).
      }
    }
  }
}

key_vault = {
  disk_encryption   = true
  soft_del_ret_days = 7
  purge_prot        = false
  sku_name          = "standard"
}

service_bus = {
  internal = {
    capacity         = 1
    sku_tier         = "Premium"
    max_payload_size = "100mb"
    topics = {
      cohort-distribution = {
        batched_operations_enabled = true
        subscribers                = ["DistributeParticipant"]
      }
      create-exception = {
        batched_operations_enabled = true
        subscribers                = ["CreateException"]
      }
      participant-management = {
        batched_operations_enabled = true
        subscribers                = ["ManageParticipant"]
      }
      servicenow-participant-management = {
        batched_operations_enabled = true
        subscribers                = ["ManageServiceNowParticipant"]
      }
    }
  }
}

sqlserver = {
  sql_admin_group_name                 = "sqlsvr_cohman_dev_uks_admin"
  ad_auth_only                         = true
  auditing_policy_retention_in_days    = 30
  security_alert_policy_retention_days = 30
  db_management_mi_name_prefix         = "mi-cohort-manager-db-management"

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
      sku                  = "S2"
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
      nems-updates = {
        container_name = "nems-updates"
      }
      nems-config = {
        container_name = "nems-config"
      }
      nems-poison = {
        container_name = "nems-poison"
      }
    }
  }
}

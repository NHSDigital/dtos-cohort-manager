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
  Project = "Cohort-Manager"
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
    }
  }
}

routes = {
  uksouth = {
    application_rules = []
    nat_rules         = []
    network_rules = [
      {
        name                  = "AllowCohmanToAudit"
        priority              = 800
        action                = "Allow"
        rule_name             = "CohmanToAudit"
        source_addresses      = ["10.2.0.0/16"] # will be populated with the cohort manager subnet address space
        destination_addresses = ["10.3.0.0/16"] # will be populated with the audit subnet address space
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      }
    ]
    route_table_routes = [
      {
        name                   = "CohmanToAudit"
        address_prefix         = "" # will be populated with the cohort manager subnet address space
        next_hop_type          = "VirtualAppliance"
        next_hop_in_ip_address = "" # will be populated with the Firewall Private IP address
      }
    ]
  }
}

app_service_plan = {
  os_type                  = "Linux"
  sku_name                 = "P2v3"
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
    CaasIntegration = {
      autoscale_override = {
        memory_percentage = {
          metric = "MemoryPercentage"

          capacity_min = "2"
          capacity_max = "10"
          capacity_def = "2"
        }
      }
    }
    CohortDistributionServices    = {}
    DemographicServices           = {}
    ExceptionHandling             = {}
    ParticipantManagementServices = {}
    ScreeningValidationService    = {}
    screeningDataServices         = {}
  }
}

function_apps = {
  acr_mi_name = "dtos-cohort-manager-acr-push"
  acr_name    = "acrukshubprodcohman"
  acr_rg_name = "rg-hub-prod-uks-cohman"

  app_insights_name    = "appi-pre-uks-cohman"
  app_insights_rg_name = "rg-cohman-pre-uks-audit"

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

  fa_config = {
    ReceiveCaasFile = {
      name_suffix                  = "receive-caas-file"
      function_endpoint_name       = "ReceiveCaasFile"
      app_service_plan_key         = "CaasIntegration"
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
      app_service_plan_key         = "CaasIntegration"
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
      app_service_plan_key   = "CaasIntegration"
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
      app_service_plan_key   = "ParticipantManagementServices"
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
      app_service_plan_key   = "ParticipantManagementServices"
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
      app_service_plan_key   = "ParticipantManagementServices"
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
      app_service_plan_key   = "screeningDataServices"
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
      app_service_plan_key   = "screeningDataServices"
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
      app_service_plan_key   = "screeningDataServices"
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
      app_service_plan_key   = "screeningDataServices"
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
      app_service_plan_key   = "ExceptionHandling"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    GetValidationExceptions = {
      name_suffix            = "get-validation-exceptions"
      function_endpoint_name = "GetValidationExceptions"
      app_service_plan_key   = "screeningDataServices"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    DemographicDataService = {
      name_suffix            = "demographic-data-service"
      function_endpoint_name = "DemographicDataService"
      app_service_plan_key   = "screeningDataServices"
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
      app_service_plan_key         = "ScreeningValidationService"
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
      app_service_plan_key   = "ScreeningValidationService"
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
      app_service_plan_key   = "ScreeningValidationService"
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
      app_service_plan_key   = "DemographicServices"
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
      app_service_plan_key   = "CohortDistributionServices"
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
      app_service_plan_key   = "CohortDistributionServices"
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
      app_service_plan_key   = "CohortDistributionServices"
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
      app_service_plan_key   = "CohortDistributionServices"
      app_urls               = []
    }

    AllocateServiceProvider = {
      name_suffix            = "allocate-service-provider"
      function_endpoint_name = "AllocateServiceProviderToParticipantByService"
      app_service_plan_key   = "CohortDistributionServices"
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
      app_service_plan_key   = "CohortDistributionServices"
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
      app_service_plan_key   = "CohortDistributionServices"
      db_connection_string   = "DtOsDatabaseConnectionString"
      app_urls               = []
    }

    ValidateCohortDistributionRecord = {
      name_suffix            = "validate-cohort-distribution-record"
      function_endpoint_name = "ValidateCohortDistributionRecord"
      app_service_plan_key   = "CohortDistributionServices"
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
      app_service_plan_key   = "CohortDistributionServices"
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
      app_service_plan_key   = "ScreeningValidationService"
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
      app_service_plan_key   = "CohortDistributionServices"
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

function_app_slots = [
  {
    function_app_slots_name   = "staging"
    function_app_slot_enabled = true
  }
]

key_vault = {
  disk_encryption   = true
  soft_del_ret_days = 7
  purge_prot        = false
  sku_name          = "standard"
}

sqlserver = {
  sql_uai_name         = "dtos-cohort-manager-sql-adm"
  sql_admin_group_name = "sqlsvr_cohman_preprod_uks_admin"
  ad_auth_only         = true

  server = {
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

  fw_rules = {}
}

storage_accounts = {
  fnapp = {
    name_suffix                   = "fnappstor"
    account_tier                  = "Standard"
    replication_type              = "LRS"
    public_network_access_enabled = false
    containers                    = {}
  }
  file_exceptions = {
    name_suffix                   = "filexptns"
    account_tier                  = "Standard"
    replication_type              = "LRS"
    public_network_access_enabled = false
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

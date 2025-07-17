application           = "cohman"
application_full_name = "cohort-manager"
environment           = "PROD"

tags = {
  Environment = "production"
}


features = {
  private_endpoints_enabled              = true
  private_service_connection_is_manual   = false
  public_network_access_enabled          = false
  log_analytics_data_export_rule_enabled = true
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.5.0.0/16"
    connect_peering   = true
    subnets = {
      pep = {
        cidr_newbits = 8
        cidr_offset  = 1
      }
    }
  }
}

app_insights = {
  appinsights_type = "web"
}

law = {
  export_enabled = true
  law_sku        = "PerGB2018"
  retention_days = 30
  export_table_names = [
    "Alert",
    "AppDependencies",
    "AppExceptions",
    "AppMetrics",
    "AppPerformanceCounters",
    "AppRequests",
    "AppSystemEvents",
    "AppTraces",
    "AzureDiagnostics",
    "AzureMetrics",
    "FunctionAppLogs",
    "LAQueryLogs",
    "StorageBlobLogs",
    "StorageFileLogs",
    "StorageQueueLogs",
    "StorageTableLogs",
    "Usage"
  ]
}

storage_accounts = {
  sqllogs = {
    name_suffix                             = "sqllogs"
    account_tier                            = "Standard"
    replication_type                        = "LRS"
    public_network_access_enabled           = false
    blob_properties_delete_retention_policy = 7
    blob_properties_versioning_enabled      = true
    containers = {
      vulnerability-assessment = {
        container_name        = "vulnerability-assessment"
        container_access_type = "private"
      }
    }
  }
}

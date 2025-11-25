application           = "cohman"
application_full_name = "cohort-manager"
environment           = "SBRK"

tags = {
  Environment = "sandbox"
}


features = {
  private_endpoints_enabled              = true
  private_service_connection_is_manual   = false
  public_network_access_enabled          = false
  log_analytics_data_export_rule_enabled = false
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.129.0.0/16"
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
  law_sku            = "PerGB2018"
  retention_days     = 30
  export_enabled     = false
  export_table_names = ["Alert"]
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
  sqlbackups = {
    name_suffix                             = "sqlbackups"
    account_tier                            = "Standard"
    replication_type                        = "LRS"
    public_network_access_enabled           = false
    blob_properties_delete_retention_policy = 7
    blob_properties_versioning_enabled      = true
    containers = {
      sql-backups-immutable = {
        container_name        = "sql-backups-immutable"
        container_access_type = "private"
        immutability_policy = {
          is_locked                           = false
          immutability_period_in_days         = 1
          protected_append_writes_all_enabled = false
          protected_append_writes_enabled     = false
        }
      }
    }
  }
}

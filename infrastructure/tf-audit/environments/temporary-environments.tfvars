application           = "cohman"
application_full_name = "cohort-manager"
#environment           = "TEMP01"   #This comes from the pipeline

# rbac_principal_name = "dtos_team_select_temp_environments"

features = {
  private_endpoints_enabled              = false
  private_service_connection_is_manual   = false
  public_network_access_enabled          = true
  log_analytics_data_export_rule_enabled = false
}

tags = {
  Project = "Cohort-Manager"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.255.0.0/16"
    connect_peering   = false
    subnets           = {}
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
    name_suffix                   = "sqllogs"
    account_tier                  = "Standard"
    replication_type              = "LRS"
    public_network_access_enabled = true
    containers = {
      vulnerability-assessment = {
        container_name        = "vulnerability-assessment"
        container_access_type = "private"
      }
    }

  }
}

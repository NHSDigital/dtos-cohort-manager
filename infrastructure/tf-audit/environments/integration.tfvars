application           = "cohman"
application_full_name = "cohort-manager"
environment           = "INT"

features = {
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
    address_space     = "10.106.0.0/16"
    connect_peering   = true
    subnets = {
      # apps = {
      #   cidr_newbits               = 8
      #   cidr_offset                = 2
      #   delegation_name            = "Microsoft.Web/serverFarms"
      #   service_delegation_name    = "Microsoft.Web/serverFarms"
      #   service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
      # }
      pep = {
        cidr_newbits = 8
        cidr_offset  = 1
      }
    }
  }
}

app_insights = {
  #name_suffix        = "cohman"
  appinsights_type = "web"
}

law = {
  #name_suffix        = "cohman"
  law_sku        = "PerGB2018"
  retention_days = 30
}

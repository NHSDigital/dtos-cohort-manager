application           = "cohman"
application_full_name = "cohort-manager"
environment           = "PRE"

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
    address_space     = "10.3.0.0/16"
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

routes = {
  uksouth = {
    application_rules = []
    nat_rules         = []
    network_rules = [
      {
        name                  = "AllowAuditToCohman"
        priority              = 800
        action                = "Allow"
        rule_name             = "AuditToCohman"
        source_addresses      = ["10.3.0.0/16"] # will be populated with the cohort manager subnet address space
        destination_addresses = ["10.2.0.0/16"] # will be populated with the audit subnet address space
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      }
    ]
    route_table_routes = [
      {
        name                   = "AuditToCohman"
        address_prefix         = "" # will be populated with the cohort manager subnet address space
        next_hop_type          = "VirtualAppliance"
        next_hop_in_ip_address = "" # will be populated with the Firewall Private IP address
      }
    ]
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
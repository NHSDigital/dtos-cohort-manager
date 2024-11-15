variable "AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_NAME" {
  description = "The name of the Azure Storage Account for the audit backend"
  type        = string
}

variable "AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME" {
  description = "The name of the container in the Audit Azure Storage Account for the backend"
  type        = string
}

variable "AUDIT_BACKEND_AZURE_STORAGE_ACCOUNT_KEY" {
  description = "The name of the Statefile for the audit  resources"
  type        = string
}

variable "AUDIT_BACKEND_AZURE_RESOURCE_GROUP_NAME" {
  description = "The name of the audit resource group for the Azure Storage Account"
  type        = string
}

variable "AUDIT_SUBSCRIPTION_ID" {
  description = "ID of the Audit subscription to deploy infrastructure"
  type        = string
}

variable "HUB_BACKEND_AZURE_STORAGE_ACCOUNT_NAME" {
  description = "The name of the Azure Storage Account for the backend"
  type        = string
}

variable "HUB_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME" {
  description = "The name of the container in the Azure Storage Account for the backend"
  type        = string
}

variable "HUB_BACKEND_AZURE_STORAGE_ACCOUNT_KEY" {
  description = "The name of the Statefile for the hub resources"
  type        = string
}

variable "HUB_BACKEND_AZURE_RESOURCE_GROUP_NAME" {
  description = "The name of the resource group for the Azure Storage Account"
  type        = string
}

variable "HUB_SUBSCRIPTION_ID" {
  description = "ID of the subscription hosting the DevOps resources"
  type        = string
}

variable "TARGET_SUBSCRIPTION_ID" {
  description = "ID of a subscription to deploy infrastructure"
  type        = string
}

variable "application" {
  description = "Project/Application code for deployment"
  type        = string
  default     = "DToS"
}

variable "application_full_name" {
  description = "Full name of the Project/Application code for deployment"
  type        = string
  default     = "DToS"
}

variable "environment" {
  description = "Environment code for deployments"
  type        = string
  default     = "DEV"
}

variable "features" {
  description = "Feature flags for the deployment"
  type        = map(bool)
}

variable "regions" {
  type = map(object({
    address_space     = optional(string)
    is_primary_region = bool
    connect_peering   = optional(bool, false)
    subnets = optional(map(object({
      cidr_newbits               = string
      cidr_offset                = string
      create_nsg                 = optional(bool, true) # defaults to true
      name                       = optional(string)     # Optional name override
      delegation_name            = optional(string)
      service_delegation_name    = optional(string)
      service_delegation_actions = optional(list(string))
    })))
  }))
}

### Cohort Manager specific variables ###
variable "app_service_plan" {
  description = "Configuration for the app service plan"
  type = object({
    sku_name                 = optional(string, "P2v3")
    os_type                  = optional(string, "Linux")
    vnet_integration_enabled = optional(bool, false)

    autoscale = object({
      memory_percentage = object({
        metric              = optional(string)
        capacity_min        = optional(string)
        capacity_max        = optional(string)
        capacity_def        = optional(string)
        time_grain          = optional(string)
        statistic           = optional(string)
        time_window         = optional(string)
        time_aggregation    = optional(string)
        inc_operator        = optional(string)
        inc_threshold       = optional(number)
        inc_scale_direction = optional(string)
        inc_scale_type      = optional(string)
        inc_scale_value     = optional(number)
        inc_scale_cooldown  = optional(string)
        dec_operator        = optional(string)
        dec_threshold       = optional(number)
        dec_scale_direction = optional(string)
        dec_scale_type      = optional(string)
        dec_scale_value     = optional(number)
        dec_scale_cooldown  = optional(string)
      })
    })

    instances = map(object({
      autoscale_override = optional(object({
        memory_percentage = object({
          metric              = optional(string)
          capacity_min        = optional(string)
          capacity_max        = optional(string)
          capacity_def        = optional(string)
          time_grain          = optional(string)
          statistic           = optional(string)
          time_window         = optional(string)
          time_aggregation    = optional(string)
          inc_operator        = optional(string)
          inc_threshold       = optional(number)
          inc_scale_direction = optional(string)
          inc_scale_type      = optional(string)
          inc_scale_value     = optional(number)
          inc_scale_cooldown  = optional(string)
          dec_operator        = optional(string)
          dec_threshold       = optional(number)
          dec_scale_direction = optional(string)
          dec_scale_type      = optional(string)
          dec_scale_value     = optional(number)
          dec_scale_cooldown  = optional(string)
        })
      }))
    }))
  })
}

variable "function_apps" {
  description = "Configuration for function apps"
  type = object({
    acr_mi_name                   = string
    acr_name                      = string
    acr_rg_name                   = string
    always_on                     = bool
    app_insights_name             = string
    app_insights_rg_name          = string
    cont_registry_use_mi          = bool
    docker_CI_enable              = string
    docker_env_tag                = string
    docker_img_prefix             = string
    enable_appsrv_storage         = bool
    ftps_state                    = string
    https_only                    = bool
    remote_debugging_enabled      = bool
    storage_uses_managed_identity = bool
    worker_32bit                  = bool
    slots = optional(map(object({
      name         = string
      slot_enabled = optional(bool, false)
    })))
    fa_config = map(object({
      name_suffix                  = string
      function_endpoint_name       = string
      app_service_plan_key         = string
      storage_account_env_var_name = optional(string, "")
      storage_containers = optional(list(object
        ({
          env_var_name   = string
          container_name = string
      })), [])
      db_connection_string = optional(string, "")
      key_vault_url        = optional(string, "")
      app_urls = optional(list(object({
        env_var_name     = string
        function_app_key = string
      })), [])
      env_vars_static = optional(list(object({
        env_var_name  = string
        env_var_value = string
      })), [])
    }))
  })
}

variable "key_vault" {
  description = "Configuration for the key vault"
  type = object({
    disk_encryption   = optional(bool, true)
    soft_del_ret_days = optional(number, 7)
    purge_prot        = optional(bool, false)
    sku_name          = optional(string, "standard")
  })
}

variable "network_security_group_rules" {
  description = "The network security group rules."
  default     = {}
  type = map(list(object({
    name                       = string
    priority                   = number
    direction                  = string
    access                     = string
    protocol                   = string
    source_port_range          = string
    destination_port_range     = string
    source_address_prefix      = string
    destination_address_prefix = string
  })))
}

/*
  application_rule_collection = [
    {
      name      = "example-application-rule-collection-1"
      priority  = 600
      action    = "Allow"
      rule_name = "example-rule-1"
      protocols = [
        {
          type = "Http"
          port = 80
        },
        {
          type = "Https"
          port = 443
        }
      ]
      source_addresses  = ["0.0.0.0/0"]
      destination_fqdns = ["example.com"]
    },
*/


variable "routes" {
  description = "Routes configuration for different regions"
  type = map(object({
    bgp_route_propagation_enabled = optional(bool, false)
    firewall_policy_priority      = number
    application_rules = list(object({
      name      = optional(string)
      priority  = optional(number)
      action    = optional(string)
      rule_name = optional(string)
      protocols = list(object({
        type = optional(string)
        port = optional(number)
      }))
      source_addresses  = optional(list(string))
      destination_fqdns = list(string)
    }))
    nat_rules = list(object({
      name                = optional(string)
      priority            = optional(number)
      action              = optional(string)
      rule_name           = optional(string)
      protocols           = list(string)
      source_addresses    = list(string)
      destination_address = optional(string)
      destination_ports   = list(string)
      translated_address  = optional(string)
      translated_port     = optional(string)
    }))
    network_rules = list(object({
      name                  = optional(string)
      priority              = optional(number)
      action                = optional(string)
      rule_name             = optional(string)
      source_addresses      = optional(list(string))
      destination_addresses = optional(list(string))
      protocols             = optional(list(string))
      destination_ports     = optional(list(string))
    }))
    route_table_routes_to_audit = list(object({
      name                   = optional(string)
      address_prefix         = optional(string)
      next_hop_type          = optional(string)
      next_hop_in_ip_address = optional(string)
    }))
    route_table_routes_from_audit = list(object({
      name                   = optional(string)
      address_prefix         = optional(string)
      next_hop_type          = optional(string)
      next_hop_in_ip_address = optional(string)
    }))
  }))
  default = {}
}

variable "sqlserver" {
  description = "Configuration for the Azure MSSQL server instance and a default database "
  type = object({

    sql_uai_name         = optional(string)
    sql_admin_group_name = optional(string)
    ad_auth_only         = optional(bool)
    retention_in_days = optional(number)        

    # Server Instance
    server = optional(object({
      sqlversion                    = optional(string, "12.0")
      tlsversion                    = optional(number, 1.2)
      azure_services_access_enabled = optional(bool, true)
    }), {})

    # Database
    dbs = optional(map(object({
      db_name_suffix = optional(string, "cohman")
      collation      = optional(string, "SQL_Latin1_General_CP1_CI_AS")
      licence_type   = optional(string, "LicenseIncluded")
      max_gb         = optional(number, 5)
      read_scale     = optional(bool, false)
      sku            = optional(string, "S0")
    })), {})

    # FW Rules
    fw_rules = optional(map(object({
      fw_rule_name = string
      start_ip     = string
      end_ip       = string
    })), {})
  })
}

variable "storage_accounts" {
  description = "Configuration for the Storage Account, currently used for Function Apps"
  type = map(object({
    name_suffix                   = string
    account_tier                  = optional(string, "Standard")
    replication_type              = optional(string, "LRS")
    public_network_access_enabled = optional(bool, false)
    containers = optional(map(object({
      container_name        = string
      container_access_type = optional(string, "private")
    })), {})
  }))
}

variable "tags" {
  description = "Default tags to be applied to resources"
  type        = map(string)
}

variable "function_app_slots" {
  description = "function app slots"
  type = list(object({
    function_app_slots_name   = optional(string, "staging")
    function_app_slot_enabled = optional(bool, false)
  }))
}

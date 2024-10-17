variable "TARGET_SUBSCRIPTION_ID" {
  description = "ID of a subscription to deploy infrastructure"
  type        = string
}

variable "AUDIT_SUBSCRIPTION_ID" {
  description = "ID of the Audit subscription to deploy infrastructure"
  type        = string
}

variable "ACR_SUBSCRIPTION_ID" {
  description = "ID of the subscription hosting the ACR used in current environment"
  type        = string
}

variable "DEVHUB_SUBSCRIPTION_ID" {
  description = "ID of the subscription hosting the DevOps resources"
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

variable "HUB_BACKEND_AZURE_STORAGE_KEY" {
  description = "The name of the Statefile for the hub resources"
  type        = string
}

variable "HUB_BACKEND_AZURE_RESOURCE_GROUP_NAME" {
  description = "The name of the resource group for the Azure Storage Account"
  type        = string
}

variable "application" {
  description = "Project/Application code for deployment"
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

variable "location" {
  description = "Location code for deployments"
  type        = string
  default     = "uksouth"
}

variable "regions" {
  type = map(object({
    address_space     = optional(string)
    is_primary_region = bool
    create_peering    = optional(bool, false)
    subnets = optional(map(object({
      cidr_newbits = string
      cidr_offset  = string
      create_nsg   = optional(bool)   # defaults to true
      name         = optional(string) # Optional name override
    })))
  }))
}

variable "resource_groups" {
  description = "Map of resource groups"
  type = map(object({
    name     = optional(string, "rg-cohort-manager-dev-suk")
    location = optional(string, "uksouth")
  }))
}

variable "resource_groups_audit" {
  description = "Map of resource groups in Audit subscription"
  type = map(object({
    name     = optional(string, "rg-audit-cohort-manager-dev-uks")
    location = optional(string, "uksouth")
  }))
}

### Cohort Manager specific variables ###

variable "acr" {
  description = "Configuration of the Azure Container Registry"
  type = object({
    resource_group_key = optional(string, "cohman")
    sku                = optional(string, "Premium")
    admin_enabled      = optional(bool, false)
    uai_name           = optional(string, "dtos-cohort-manager-acr-push")
  })
}

variable "api_management" {
  description = "Configuration of the API Management Service"
  type = object({
    resource_group_key = optional(string, "cohman")
    sku                = optional(string, "Basic_1")
    publisher_name     = optional(string, "NHS_DToS_CohortManager")
    publisher_email    = optional(string, "maciej.murawski@nordcloud.com")
  })
}

variable "app_insights" {
  description = "Configuration of the App Insights"
  type = object({
    name_suffix              = optional(string, "cohman")
    resource_group_key       = optional(string, "cohman")
    appinsights_type         = optional(string, "web")
    audit_resource_group_key = optional(string, "audit")
  })
}

variable "app_service_plan" {
  description = "Configuration for the app service plan"
  type = object({
    resource_group_key = optional(string, "cohman")
    sku_name           = optional(string, "P2v3")
    os_type            = optional(string, "Linux")

    autoscale = object({
      memory_percentage = object({
        metric              = optional(string, "MemoryPercentage")
        capacity_min        = optional(string, "1")
        capacity_max        = optional(string, "5")
        capacity_def        = optional(string, "1")
        time_grain          = optional(string, "PT1M")
        statistic           = optional(string, "Average")
        time_window         = optional(string, "PT10M")
        time_aggregation    = optional(string, "Average")
        inc_operator        = optional(string, "GreaterThan")
        inc_threshold       = optional(number, 70)
        inc_scale_direction = optional(string, "Increase")
        inc_scale_type      = optional(string, "ChangeCount")
        inc_scale_value     = optional(number, 1)
        inc_scale_cooldown  = optional(string, "PT5M")
        dec_operator        = optional(string, "LessThan")
        dec_threshold       = optional(number, 25)
        dec_scale_direction = optional(string, "Decrease")
        dec_scale_type      = optional(string, "ChangeCount")
        dec_scale_value     = optional(number, 1)
        dec_scale_cooldown  = optional(string, "PT5M")
      })
    })
  })
}

variable "event_grid" {
  description = "Configuration for the event grid"
  type = object({
    topic = object({
      resource_group_key = optional(string, "cohman")
      name_suffix        = optional(string, "cohman")
    })
  })
}

variable "function_apps" {
  description = "Configuration for function apps"
  type = object({
    resource_group_key       = string
    acr_mi_name              = string
    acr_name                 = string
    acr_rg_name              = string
    cont_registry_use_mi     = bool
    docker_CI_enable         = string
    docker_env_tag           = string
    docker_img_prefix        = string
    enable_appsrv_storage    = bool
    ftps_state               = string
    https_only               = bool
    remote_debugging_enabled = bool
    worker_32bit             = bool
    fa_config = map(object({
      name_suffix                  = string
      function_endpoint_name       = string
      storage_account_env_var_name = optional(string, "")
      storage_containers = optional(list(object
        ({
          env_var_name   = string
          container_name = string
      })), [])
      db_connection_string = optional(string, "")
      app_urls = optional(list(object({
        env_var_name     = string
        function_app_key = string
      })), [])
    }))
  })
}

variable "key_vault" {
  description = "Configuration for the key vault"
  type = object({
    resource_group_key = optional(string, "cohman")
    disk_encryption    = optional(bool, true)
    soft_del_ret_days  = optional(number, 7)
    purge_prot         = optional(bool, false)
    sku_name           = optional(string, "standard")
  })
}

variable "law" {
  description = "Configuration of the Log Analytics Workspace"
  type = object({
    name_suffix              = optional(string, "cohman")
    resource_group_key       = optional(string, "cohman")
    law_sku                  = optional(string, "PerGB2018")
    retention_days           = optional(number, 30)
    audit_resource_group_key = optional(string, "audit")
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

variable "sqlserver" {
  description = "Configuration for the Azure MSSQL server instance and a default database "
  type = object({

    sql_uai_name       = optional(string, "dtos-cohort-manager-sql-adm")
    sql_adm_group_name = optional(string, "sqlsvr_cohman_dev_uks_admin")
    ad_auth_only       = optional(bool, true)

    # Server Instance
    server = object({
      resource_group_key            = optional(string, "cohman")
      sqlversion                    = optional(string, "12.0")
      tlsversion                    = optional(number, 1.2)
      azure_services_access_enabled = optional(bool, true)
    })

    # Database
    dbs = map(object({
      db_name_suffix = optional(string, "cohman")
      collation      = optional(string, "SQL_Latin1_General_CP1_CI_AS")
      licence_type   = optional(string, "LicenseIncluded")
      max_gb         = optional(number, 5)
      read_scale     = optional(bool, false)
      sku            = optional(string, "S0")
    }))

    # FW Rules
    fw_rules = map(object({
      fw_rule_name = optional(string, "AllowAccessFromAzure")
      start_ip     = optional(string, "0.0.0.0")
      end_ip       = optional(string, "0.0.0.0")
    }))
  })
}

variable "storage_accounts" {
  description = "Configuration for the Storage Account, currently used for Function Apps"
  type = object({
    resource_group_key = optional(string, "cohman")
    sa_config = map(object({
      name_suffix                   = optional(string, "fnappstor")
      account_tier                  = optional(string, "Standard")
      replication_type              = optional(string, "LRS")
      public_network_access_enabled = optional(bool, true)
    }))
    cont_config = map(object({
      sa_key           = optional(string, "file_exceptions")
      cont_name        = optional(string, "config")
      cont_access_type = optional(string, "private")
    }))
  })
}

variable "tags" {
  description = "Default tags to be applied to resources"
  type        = map(string)
}

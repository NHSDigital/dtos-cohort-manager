variable "TARGET_SUBSCRIPTION_ID" {
  description = "ID of a subscription to deploy infrastructure"
  type        = string
}

variable "AUDIT_SUBSCRIPTION_ID" {
  description = "ID of the Audit subscription to deploy infrastructure"
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

variable "location" {
  description = "Location code for deployments"
  type        = string
  default     = "uksouth"
}

variable "tags" {
  description = "Default tags to be applied to resources"
  type        = map(string)
}

variable "resource_groups" {
  description = "Map of resource groups"
  type = map(object({
    name = optional(string, "rg-cohort-manager-dev-suk")
  }))
}

variable "storage_accounts" {
  description = "Configuration for the Storage Account, currently used for Function Apps"
  type = object({
    fnapp = object({
      name_suffix                   = optional(string, "fnappstor")
      resource_group_key            = optional(string, "cohman")
      account_tier                  = optional(string, "Standard")
      replication_type              = optional(string, "LRS")
      public_network_access_enabled = optional(bool, true)
    })
    file_exceptions = object({
      name_suffix                   = optional(string, "filexptns")
      resource_group_key            = optional(string, "cohman")
      account_tier                  = optional(string, "Standard")
      replication_type              = optional(string, "LRS")
      public_network_access_enabled = optional(bool, true)
      cont_name                     = optional(string, "file-exceptions")
      cont_access_type              = optional(string, "private")
    })
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

variable "app_service_plan" {
  description = "Configuration for the app service plan"
  type = object({
    resource_group_key = optional(string, "cohman")
    sku_name           = optional(string, "P2v3")
    os_type            = optional(string, "Linux")
  })
}

variable "function_app" {
  description = "Configuration for the function app"
  type = object({
    resource_group_key       = optional(string, "baseline")
    gl_worker_32bit          = optional(bool, false)
    gl_docker_env_tag        = optional(string, "development")
    gl_docker_CI_enable      = optional(string, "cohort-manager")
    gl_docker_img_prefix     = optional(string, "false")
    gl_cont_registry_use_mi  = optional(bool, true)
    gl_enable_appsrv_storage = optional(string, "false")

    fa_config = map(object({
      name_suffix = string
    }))
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

variable "law" {
  description = "Configuration of the Log Analytics Workspace"
  type = object({
    name_suffix              = optional(string, "cohman")
    resource_group_key       = optional(string, "cohman")
    law_sku                  = optional(string, "PerGB2018")
    retention_days           = optional(number, 30)
    audit_resource_group_key = optional(string, "audit_dev")
  })
}

variable "app_insights" {
  description = "Configuration of the App Insights"
  type = object({
    name_suffix              = optional(string, "cohman")
    resource_group_key       = optional(string, "cohman")
    appinsights_type         = optional(string, "web")
    audit_resource_group_key = optional(string, "audit_dev")
  })
}

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

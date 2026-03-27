variable "TARGET_SUBSCRIPTION_ID" {
  description = "ID of a subscription to deploy infrastructure"
  type        = string
}

variable "HUB_SUBSCRIPTION_ID" {
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

variable "HUB_BACKEND_AZURE_STORAGE_ACCOUNT_KEY" {
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

variable "app_insights" {
  description = "Configuration of the App Insights"
  type = object({
    name             = optional(string, "cohman")
    appinsights_type = optional(string, "web")
  })
}

variable "law" {
  description = "Configuration of the Log Analytics Workspace"
  type = object({
    name               = optional(string, "cohman")
    law_sku            = optional(string, "PerGB2018")
    retention_days     = optional(number, 30)
    export_enabled     = optional(bool, false)
    export_table_names = optional(list(string), [])
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

variable "storage_accounts" {
  description = "Configuration for the Storage Account, currently used for SQL Server audit logs"
  type = map(object({
    name_suffix                   = string
    account_tier                  = optional(string, "Standard")
    replication_type              = optional(string, "LRS")
    public_network_access_enabled = optional(bool, false)
    access_tier                   = optional(string, "Hot")
    containers = optional(map(object({
      container_name        = string
      container_access_type = optional(string, "private")
      immutability_policy = optional(object({
        is_locked                           = optional(bool, false)
        immutability_period_in_days         = optional(number, 0)
        protected_append_writes_all_enabled = optional(bool, false)
        protected_append_writes_enabled     = optional(bool, false)
      }), null)
    })), {})
  }))
}

variable "tags" {
  description = "Default tags to be applied to resources"
  type        = map(string)
  default     = {}
}

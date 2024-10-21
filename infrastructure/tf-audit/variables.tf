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

variable "resource_groups_audit" {
  description = "Map of resource groups in Audit subscription"
  type = map(object({
    name     = optional(string, "rg-audit-cohort-manager-dev-uks")
    location = optional(string, "uksouth")
  }))
}

### Cohort Manager specific variables ###

variable "app_insights" {
  description = "Configuration of the App Insights"
  type = object({
    name                     = optional(string, "cohman")
    resource_group_key       = optional(string, "cohman")
    appinsights_type         = optional(string, "web")
    audit_resource_group_key = optional(string, "audit")
  })
}

variable "law" {
  description = "Configuration of the Log Analytics Workspace"
  type = object({
    name                     = optional(string, "cohman")
    resource_group_key       = optional(string, "cohman")
    law_sku                  = optional(string, "PerGB2018")
    retention_days           = optional(number, 30)
    audit_resource_group_key = optional(string, "audit")
  })
}

variable "tags" {
  description = "Default tags to be applied to resources"
  type        = map(string)
}

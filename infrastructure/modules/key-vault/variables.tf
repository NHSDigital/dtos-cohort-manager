variable "name" {
  type = string
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the Key Vault. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the  Key Vault is created."
}

variable "disk_encryption" {
  type        = bool
  description = "Should the disk encryption be enabled"
  default     = true
}

variable "purge_protection_enabled" {
  type        = bool
  description = "Should the purge protection be enabled."
  default     = false
}

variable "soft_delete_retention" {
  type        = number
  description = "Name of the  Key Vault which is created."
  default     = "7"
}

variable "sku_name" {
  type        = string
  description = "Type of the Key Vault's SKU."
  default     = "standard"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

variable "private_endpoint_properties" {
  description = "Consolidated properties for the Key Vault Private Endpoint."
  type = object({
    private_dns_zone_ids_keyvault        = optional(list(string), [])
    private_endpoint_enabled             = optional(bool, false)
    private_endpoint_subnet_id           = optional(string, "")
    private_endpoint_resource_group_name = optional(string, "")
    private_service_connection_is_manual = optional(bool, false)
  })
}

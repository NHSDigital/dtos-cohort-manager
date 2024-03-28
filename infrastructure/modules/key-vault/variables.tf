variable "names" {
  type = map(string)
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

variable "soft_delete_retention" {
  type        = number
  description = "Name of the  Key Vault which is created."
  default     = "7"
}

variable "purge_protection_enabled" {
  type        = bool
  description = "Should the purge protection be enabled."
  default     = false
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

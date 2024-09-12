variable "is_storage_private_dns_zone_enabled" {
  type        = bool
  default     = false
  description = "To create storage private DNS zone or not."
}

variable "is_function_app_private_dns_zone_enabled" {
  type        = bool
  default     = false
  description = "To create a function app private DNS zone or not."
}

variable "is_azure_sql_private_dns_zone_enabled" {
  type        = bool
  default     = false
  description = "To create a Azure SQL private DNS zone or not."
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the NSG. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "location"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

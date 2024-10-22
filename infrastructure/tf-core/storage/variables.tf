variable "name" {
  type        = string
  description = "The name of the Storage Account"

  validation {
    condition     = length(var.name) <= 24
    error_message = "The Storage Account name must be between 3 and 24 characters in length."
  }
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the Storage Account. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the Storage Account is created."
}

variable "account_replication_type" {
  type        = string
  description = "The type of replication to use for this Storage Account. Can be either LRS, GRS, RAGRS or ZRS."
  default     = "LRS"

}

variable "account_tier" {
  type        = string
  description = "Defines the Tier to use for this storage account. Valid options are Standard and Premium."
  default     = "Standard"
}

variable "containers" {
  description = "Definition of Containers configuration"
  type = map(object({
    container_name        = string
    container_access_type = string
  }))
}

variable "private_endpoint_properties" {
  description = "Consolidated properties for the Function App Private Endpoint."
  type = object({
    private_dns_zone_ids_blob            = optional(list(string), [])
    private_dns_zone_ids_queue           = optional(list(string), [])
    private_endpoint_enabled             = optional(bool, false)
    private_endpoint_subnet_id           = optional(string, "")
    private_endpoint_resource_group_name = optional(string, "")
    private_service_connection_is_manual = optional(bool, false)
  })

  validation {
    condition     = var.private_endpoint_properties.private_endpoint_enabled == false || (length(var.private_endpoint_properties.private_dns_zone_ids_blob) > 0 && length(var.private_endpoint_properties.private_dns_zone_ids_queue) > 0 && length(var.private_endpoint_properties.private_endpoint_subnet_id) > 0)
    error_message = "Both private_dns_zone_ids and private_endpoint_subnet_id must be provided if private_endpoint_enabled is true."
  }
}

variable "public_network_access_enabled" {
  type        = bool
  description = "Controls whether data in the account may be accessed from public networks."
  default     = false
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

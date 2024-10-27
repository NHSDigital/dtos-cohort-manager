variable "name" {
  type        = string
  description = "The name (in FQDN form) of the zone."
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the zone. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the LAW is created."
}

variable "ingestion_access_mode" {
  type        = string
  description = "The access mode for the ingestion endpoint. Possible values are Private and Public."
}

variable "query_access_mode" {
  type        = string
  description = "The access mode for the query endpoint. Possible values are Private and Public."
}

variable "private_endpoint_properties" {
  description = "Consolidated properties for the Function App Private Endpoint."
  type = object({
    private_dns_zone_ids                 = optional(list(string), [])
    private_endpoint_enabled             = optional(bool, false)
    private_endpoint_subnet_id           = optional(string, "")
    private_endpoint_resource_group_name = optional(string, "")
    private_service_connection_is_manual = optional(bool, false)
  })
}

variable "tags" {
  description = "A mapping of tags to assign to the resource."
  type        = map(string)
  default     = {}
}

variable "name" {
  type        = string
  description = "The name (in FQDN form) of the zone."
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the zone. Changing this forces a new resource to be created."
}

variable "ingestion_access_mode" {
  type        = string
  description = "The access mode for the ingestion endpoint. Possible values are Private and Public."
}

variable "query_access_mode" {
  type        = string
  description = "The access mode for the query endpoint. Possible values are Private and Public."
}

variable "tags" {
  description = "A mapping of tags to assign to the resource."
  type        = map(string)
  default     = {}
}
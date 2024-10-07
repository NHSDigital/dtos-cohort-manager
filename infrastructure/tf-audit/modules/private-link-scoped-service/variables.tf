variable "name" {
  type        = string
  description = "The name (in FQDN form) of the zone."
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the zone. Changing this forces a new resource to be created."
}

variable "linked_resource_id" {
  type        = string
  description = "The ID of the resource to link to the private link service."
}

variable "scope_name" {
  type        = string
  description = "The name of the private link scope."
}

variable "route_table_name" {
  description = "The name of the route table."
  type        = string
}

variable "location" {
  description = "The location/region where the route table is created."
  type        = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which to create the route table."
  type        = string
}

variable "tags" {
  description = "A map of tags to assign to the resource."
  type        = map(string)
  default     = {}
}

variable "routes" {
  description = "A list of routes to add to the route table."
  type = list(object({
    name                   = string
    address_prefix         = string
    next_hop_type          = string
    next_hop_in_ip_address = string
  }))
  default = []
}

variable "name" {
  type        = string
  description = "The name of the nsg."
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

variable "nsg_rules" {
  description = "Default NSG rules for securing subnets according to UK OFFICIAL, UK NHS, and Azure Security Benchmark."
  type = list(object({
    name                       = string
    priority                   = number
    direction                  = string
    access                     = string
    protocol                   = string
    source_port_range          = string
    destination_port_range     = string
    source_address_prefix      = string
    destination_address_prefix = string
  }))
}

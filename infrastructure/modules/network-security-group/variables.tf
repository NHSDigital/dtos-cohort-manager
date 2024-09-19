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
  description = "Additional NSG rules for securing subnets (Optional)."
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

  validation {
    condition = length(var.nsg_rules) == 0 || alltrue([
      for rule in var.nsg_rules : (
        rule.name != "" &&
        rule.priority > 99 &&
        contains(["Inbound", "Outbound"], rule.direction) &&
        contains(["Allow", "Deny"], rule.access) &&
        contains(["Tcp", "Udp", "Icmp", "*"], rule.protocol) &&
        rule.source_port_range != "" &&
        rule.destination_port_range != "" &&
        rule.source_address_prefix != "" &&
        rule.destination_address_prefix != ""
      )
    ])
    error_message = "Each network security group rule must have a valid name, priority, direction (Inbound or Outbound), access (Allow or Deny), protocol (Tcp, Udp, Icmp, or *), source port range, destination port range, source address prefix, and destination address prefix."
  }
}

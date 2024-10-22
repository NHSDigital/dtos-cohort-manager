variable "name" {
  type        = string
  description = "The name of the vnet."
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the VNET. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = ""
}

variable "is_private_endpoint_network_policies_enabled" {
  type        = bool
  default     = false
  description = "If the private endpoint network policies are enabled or not."
}

variable "vnet_name" {
  type        = string
  default     = null
  description = "The name of the VNET subnet."
}

variable "vnet_address_space" {
  type = list(string)
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

variable "network_security_group_id" {
  type        = string
  default     = null
  description = "The ID of the Network Security Group (NSG) to associate with the subnet."
}

variable "private_endpoint_network_policies" {
  type        = bool
  default     = false # Default as per compliance requirements
  description = "Enable or disable network policies for private endpoints."
}

variable "default_nsg_rules" {
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
  default = [
    {
      name                       = "DenyAllInbound"
      priority                   = 100
      direction                  = "Inbound"
      access                     = "Deny"
      protocol                   = "*"
      source_port_range          = "*"
      destination_port_range     = "*"
      source_address_prefix      = "*"
      destination_address_prefix = "*"
    },
    {
      name                       = "AllowLoadBalancer"
      priority                   = 200
      direction                  = "Inbound"
      access                     = "Allow"
      protocol                   = "*"
      source_port_range          = "*"
      destination_port_range     = "*"
      source_address_prefix      = "AzureLoadBalancer"
      destination_address_prefix = "*"
    },
    {
      name                       = "DenyAllOutbound"
      priority                   = 1000
      direction                  = "Outbound"
      access                     = "Deny"
      protocol                   = "*"
      source_port_range          = "*"
      destination_port_range     = "*"
      source_address_prefix      = "*"
      destination_address_prefix = "*"
    }
  ]
}

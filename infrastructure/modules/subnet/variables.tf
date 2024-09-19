variable "address_prefixes" {
  type        = list(string)
  description = "The address prefixes for the subnet."
}

variable "delegation_name" {
  type        = string
  default     = ""
  description = "The name of the delegation for the subnet."
}

variable "default_outbound_access_enabled" {
  type        = bool
  default     = true
  description = "Indicates whether the subnet has outbound access enabled."
}

variable "location" {
  type        = string
  description = ""
}

variable "name" {
  type        = string
  description = "The name of the subnet."
}

# Create this alongside the subnet
# variable "network_security_group_id" {
#   type        = string
#   description = "The ID of the Network Security Group (NSG) to associate with the subnet."
# }

variable "network_security_group_name" {
  type        = string
  description = "The name of the network security group."
}

variable "network_security_group_nsg_rules" {
  description = "The network security group rules."
  default     = []
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

variable "private_endpoint_network_policies" {
  type        = string
  default     = "Disabled" # Default as per compliance requirements
  description = "Enable or disable network policies for private endpoints."

  validation {
    condition     = contains(["Disabled", "Enabled", "NetworkSecurityGroupEnabled", "RouteTableEnabled"], var.private_endpoint_network_policies)
    error_message = "The private_endpoint_network_policies variable must be one of the following: Disabled, Enabled, NetworkSecurityGroupEnabled, RouteTableEnabled."
  }
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the VNET. Changing this forces a new resource to be created."
}

variable "service_delegation_name" {
  type        = string
  default     = ""
  description = "The name of the service delegation."
}

variable "service_delegation_actions" {
  type        = list(string)
  default     = []
  description = "The actions for the service delegation."
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

variable "vnet_name" {
  type        = string
  default     = null
  description = "The name of the VNets to which the subnet will be associated."
}

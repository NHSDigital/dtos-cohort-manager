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

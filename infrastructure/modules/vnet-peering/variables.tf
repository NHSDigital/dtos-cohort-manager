variable "allow_forwarded_traffic" {
  description = "Controls if forwarded traffic from the VMs in the linked virtual network space is allowed."
  type        = bool
}

variable "allow_gateway_transit" {
  description = "Controls gatewayLinks associated with the remote virtual network."
  type        = bool
}

variable "allow_virtual_network_access" {
  description = "Controls if the VMs in the linked virtual network space would be able to access all the VMs in the local virtual network space."
  type        = bool
}

variable "name" {
  description = "The name of the Peering connection."
  type        = string
}

variable "peer_complete_virtual_networks_enabled" {
  description = "Specifies whether complete Virtual Network address space is peered. Defaults to true. (Optional)"
  type        = bool
  default     = true
}

variable "remote_subnet_names" {
  description = "A list of remote Subnet names from remote Virtual Network that are Subnet peered. (Optional)"
  type        = list(string)
  default     = []
}

variable "remote_vnet_id" {
  description = "The ID of the remote virtual network to peer with."
  type        = string
}

variable "resource_group_name" {
  description = "The name of the resource group in which to create the peering."
  type        = string
}

variable "use_remote_gateways" {
  description = "Controls if remote gateways can be used on the linked virtual network."
  type        = bool
}

variable "vnet_name" {
  description = "The name of the local virtual network to peer with."
  type        = string
}

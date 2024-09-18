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

variable "role_definition_name" {
  type = string
}

variable "role_assignment_resource_id" {
  type    = string
  default = ""
}

variable "uai_name" {
  type        = string
  description = "The name of the user assigned identity."
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

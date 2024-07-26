
variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the ACR. Changing this forces a new resource to be created."
}

variable "names" {
  type        = map(string)
  description = "The basic part of the ACR name."
}

variable "location" {
  type        = string
  description = "The location/region where the ACR is created."
}

variable "sku" {
  type        = string
  description = "The SKU of the ACR."
}

variable "admin_enabled" {
  type        = string
  description = "Specifies whether the admin user is enabled."
}

variable "uai_name" {
  type        = string
  description = "Name of the User Assigned Identity for ACR Push"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

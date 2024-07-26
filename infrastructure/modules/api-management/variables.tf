
variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the API Management. Changing this forces a new resource to be created."
}

variable "names" {
  type        = map(string)
  description = "The basic part of the API Management name."
}

variable "location" {
  type        = string
  description = "The location/region where the API Management is created."
}

variable "sku" {
  type        = string
  description = "The SKU of the API Management."
}

variable "publisher_name" {
  type        = string
  description = "The name of publisher/company."
}

variable "publisher_email" {
  type        = string
  description = "The email of publisher/company."
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

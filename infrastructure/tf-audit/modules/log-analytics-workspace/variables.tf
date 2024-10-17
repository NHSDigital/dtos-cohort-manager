
variable "location" {
  type        = string
  description = "The location/region where the LAW is created."
}

variable "name_suffix" {
  type        = string
  description = "Is the LAW name suffix."
}

variable "law_sku" {
  type        = string
  description = "The SKU for LAW."
}

variable "retention_days" {
  type        = number
  description = "Retention days for LAW."
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "A mapping of tags to assign to the resource."
}

variable "names" {
  description = "Standard naming configuration object for sub-resources."
}

variable "audit_resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create (audit sub). Changing this forces a new resource to be created."
}

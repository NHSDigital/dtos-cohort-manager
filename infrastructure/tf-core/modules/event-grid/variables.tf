
variable "names" {
  type = map(string)
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the Event Grid Topic. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the Event Grid Topic is created."
}

variable "name_suffix" {
  type        = string
  description = "The name suffix of the Event Grid Topic. Changing this forces a new resource to be created."
  default     = "baseline"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

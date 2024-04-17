variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the AI is created."
}

variable "name_suffix" {
  type        = string
  description = "Is the App Insights workspace name."
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "A mapping of tags to assign to the resource."
}

variable "names" {
  description = "Standard naming configuration object for sub-resources."
}

variable "appinsights_type" {
  type        = string
  description = "Type of Application Insigts (default: web)."
}

variable "law_id" {
  type        = string
  description = "Is the LAW workspace ID."
}



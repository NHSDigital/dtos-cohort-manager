
variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the Function App. Changing this forces a new resource to be created."
}

variable "name" {
  type        = string
  description = "The name of the Function App."
}

variable "location" {
  type        = string
  description = "The location/region where the Function App is created."
}

variable "appsvcplan_name" {
  type        = string
  description = "The name of the AppServicePlan."
}

variable "sa_name" {
  type        = string
  description = "The name of the Storage Account."
}

variable "fnapp_count" {
  default     = 1
  description = "The counter for creating multiple function apps."
}

variable "tags" {
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

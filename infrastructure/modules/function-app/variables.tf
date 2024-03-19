
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

variable "asp_id" {
  type        = string
  description = "The ID of the AppServicePlan."
}

variable "sa_name" {
  type        = string
  description = "The name of the Storage Account."
}

variable "sa_prm_key" {
  type        = string
  description = "The Storage Account Primary Access Key."
}

variable "fnapp_count" {
  type        = number
  default     = 1
  description = "The counter for creating multiple function apps."
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

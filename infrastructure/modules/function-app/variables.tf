
variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the Function App. Changing this forces a new resource to be created."
}

variable "names" {
  type        = map(string)
  description = "The basic part of the Function App name."
}

variable "function_app" {
  description = "Definition of Function Apps configuration"
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

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

variable "ai_connstring" {
  type        = string
  description = "The App Insights connection string."
}

variable "ai_key" {
  type        = string
  description = "The App Insights instrumentation key."
}

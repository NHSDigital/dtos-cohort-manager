
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

variable "gl_worker_32bit" {
  type        = bool
  description = "Should the Windows Function App use a 32-bit worker process. Defaults to true"
}

variable "app_settings" {
  description = "App settings for App Service"
}

variable "acr_registry_url" {
  description = "The URL that can be used to log into the container registry."
}

variable "image_tag" {
  description = "Tag of the docker image"
}

variable "cont_registry_use_mi" {
  description = "Should connections for Azure Container Registry use Managed Identity."
}

variable "acr_mi_id" {
  description = "The ID of the Managed Service Identity to use for connections to the Azure Container Registry."
}

variable "acr_mi_client_id" {
  description = "The Client ID of the Managed Service Identity to use for connections to the Azure Container Registry."
}

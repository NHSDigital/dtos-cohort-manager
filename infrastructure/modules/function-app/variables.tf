
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

variable "image_tag" {
  description = "Tag of the docker image"
}

variable "cont_registry_use_mi" {
  description = "Should connections for Azure Container Registry use Managed Identity."
}

variable "caasfolder_STORAGE" {
  description = "Primary connection string to file exeptions storage account"
}

variable "docker_CI_enable" {
  description = "Is the Docker CI enabled (default - false)"
}

variable "docker_img_prefix" {
  description = "A commont part of the docker image name"
}

variable "db_name" {
  type        = string
  description = "Name of the deployed DB to which the connection string is linking."
}

variable "enable_appsrv_storage" {
  description = "If websites app service storage should be enabled"
}

variable "acr_name" {
  description = "Name of the Azure Container Registry that's connected to the Function Apps"
  type        = string
}

variable "acr_rg_name" {
  description = "Name of the resource group of Azure Container Registry that's connected to the Function Apps"
  type        = string
}

variable "acr_mi_name" {
  description = "Name of the User Assigned Managed Identiti of Azure Container Registry that's connected to the Function Apps"
  type        = string
}

######################
# security defaults
######################

variable "https_only" {
  type        = bool
  description = "Can the Function App only be accessed via HTTPS?"
  default     = true
}

variable "remote_debugging_enabled" {
  type        = bool
  description = "Should Remote Debugging be enabled."
  default     = false
}

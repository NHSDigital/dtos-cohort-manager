variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the App Service Plan. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the App Service Plan is created."
}

variable "names" {
  description = "Standard naming configuration object for sub-resources."
}

variable "os_type" {
  description = "OS type for deployed App Service Plan."
}

variable "sku_name" {
  description = "SKU name for deployed App Service Plan."
}
variable "tags" {
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the App Service Plan. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the App Service Plan is created."
}

variable "names" {
  type        = map(string)
  description = "Standard naming configuration object for sub-resources."
}

variable "os_type" {
  type        = string
  description = "OS type for deployed App Service Plan."
  default     = "Windows"
}

variable "sku_name" {
  type        = string
  description = "SKU name for deployed App Service Plan."
  default     = "B1"
}
variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

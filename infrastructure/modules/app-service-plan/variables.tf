variable "name" {
  type        = string
  description = "Name of the App Service Plan."
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the App Service Plan. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the App Service Plan is created."
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

variable "vnet_integration_enabled" {
  type        = bool
  description = "Indicates whether the App Service Plan is integrated with a VNET."
  default     = false
}

variable "vnet_integration_subnet_id" {
  type        = string
  description = "The ID of the subnet to integrate with."
  default     = ""
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

## autoscale rule ##

variable "metric" {
  type    = string
  default = "MemoryPercentage"
}
variable "capacity_min" {
  type    = string
  default = "1"
}

variable "capacity_max" {
  type    = string
  default = "5"
}

variable "capacity_def" {
  type    = string
  default = "1"
}

variable "time_grain" {
  type    = string
  default = "PT1M"
}

variable "statistic" {
  type    = string
  default = "Average"
}

variable "time_window" {
  type    = string
  default = "PT10M"
}

variable "time_aggregation" {
  type    = string
  default = "Average"
}

variable "inc_operator" {
  type    = string
  default = "GreaterThan"
}

variable "inc_threshold" {
  type    = number
  default = 70
}

variable "inc_scale_direction" {
  type    = string
  default = "Increase"
}

variable "inc_scale_type" {
  type    = string
  default = "ChangeCount"
}

variable "inc_scale_value" {
  type    = string
  default = "1"
}

variable "inc_scale_cooldown" {
  type    = string
  default = "PT5M"
}

variable "dec_operator" {
  type    = string
  default = "LessThan"
}

variable "dec_threshold" {
  type    = number
  default = 25
}

variable "dec_scale_direction" {
  type    = string
  default = "Decrease"
}

variable "dec_scale_type" {
  type    = string
  default = "ChangeCount"
}

variable "dec_scale_value" {
  type    = string
  default = "1"
}

variable "dec_scale_cooldown" {
  type    = string
  default = "PT5M"
}

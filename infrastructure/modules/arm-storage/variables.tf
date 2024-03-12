
variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the Storage Account. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the Storage Account is created."
}

variable "name" {
  type        = string
  description = "The name of the Storage Account."
}

variable "account_tier" {
  description = "."
  default     = {}
}

variable "sa_replication_type" {
  description = "."
  default     = {}
}

variable "public_access" {
  description = "."
  default     = {}
}


variable "tags" {
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

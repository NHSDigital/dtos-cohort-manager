
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
  description = "Defines the Tier to use for this storage account."
  default     = {}
}

variable "sa_replication_type" {
  description = "Defines the type of replication to use for this storage account."
  default     = {}
}

variable "public_access" {
  description = "Whether the public network access is enabled."
  default     = {}
}


variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

### File Exceptions
variable "fe_name" {
  type        = string
  description = "The name of the Storage Account."
}

variable "fe_resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the Storage Account. Changing this forces a new resource to be created."
}

variable "fe_location" {
  type        = string
  description = "The location/region where the Storage Account is created."
}

variable "fe_account_tier" {
  description = "Defines the Tier to use for this storage account."
  default     = {}
}

variable "fe_sa_replication_type" {
  description = "Defines the type of replication to use for this storage account."
  default     = {}
}

variable "fe_public_access" {
  description = "Whether the public network access is enabled."
  default     = {}
}

variable "fe_cont_name" {
  type        = string
  description = "The name of the Container which should be created within the Storage Account. Changing this forces a new resource to be created."
}

variable "fe_cont_access_type" {
  type        = string
  description = "The Access Level configured for this Container. Possible values are blob, container or private. Defaults to private."
}


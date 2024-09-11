
# variable "resource_group_name" {
#   type        = string
#   description = "The name of the resource group in which to create the Storage Account. Changing this forces a new resource to be created."
# }

# variable "location" {
#   type        = string
#   description = "The location/region where the Storage Account is created."
# }

variable "storage_accounts" {
  description = "Definition of Storage Accounts configuration"
}

variable "names" {
  type        = map(string)
  description = "The basic part of the Storage Account name."
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

variable "names" {}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the SQL Server. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the SQL Server is created."
}

variable "sqlversion" {
  description = "Varsion of SQL to be created"
}

variable "tlsver" {
  description = " The Minimum TLS Version for all SQL Database and SQL Data Warehouse databases associated with the server"
}

variable "tags" {
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

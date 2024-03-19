variable "names" {
  type = map(string)
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group in which to create the SQL Server. Changing this forces a new resource to be created."
}

variable "location" {
  type        = string
  description = "The location/region where the SQL Server is created."
}

variable "sqlversion" {
  type        = string #checkType
  description = "Varsion of SQL to be created"
  default     = "12.0"
}

variable "tlsver" {
  type        = number
  description = " The Minimum TLS Version for all SQL Database and SQL Data Warehouse databases associated with the server"
  default     = 1.2
}

variable "kv_id" {
  type        = string
  description = "Name of the Key Vault in which the admin credentials are put"
}

#db

variable "db_name_suffix" {
  type        = string
  description = "The name of the MS SQL Database. Changing this forces a new resource to be created."
  default     = "baseline"
}

variable "collation" {
  type        = string
  description = "Specifies the collation of the database. Changing this forces a new resource to be created."
  default     = "SQL_Latin1_General_CP1_CI_AS"
}

variable "licence_type" {
  type        = string
  description = " Specifies the license type applied to this database. Possible values are LicenseIncluded and BasePrice"
  default     = "LicenseIncluded"
}

variable "max_gb" {
  type        = number
  description = "The max size of the database in gigabytes"
  default     = 5
}

variable "read_scale" {
  type        = bool
  description = "If enabled, connections that have application intent set to readonly in their connection string may be routed to a readonly secondary replica. This property is only settable for Premium and Business Critical databases."
  default     = false
}

variable "sku" {
  type        = string #checkType
  description = "Specifies the name of the SKU used by the database. For example, GP_S_Gen5_2,HS_Gen4_1,BC_Gen5_2, ElasticPool, Basic,S0, P2 ,DW100c, DS100. Changing this from the HyperScale service tier to another service tier will create a new resource."
  default     = "50"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

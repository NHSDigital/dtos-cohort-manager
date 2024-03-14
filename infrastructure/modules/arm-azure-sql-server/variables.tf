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

#db

variable "db_name_suffix" {
  description = "The name of the MS SQL Database. Changing this forces a new resource to be created."
}

variable "collation" {
  description = "Specifies the collation of the database. Changing this forces a new resource to be created."
}

variable "licence_type" {
  description = " Specifies the license type applied to this database. Possible values are LicenseIncluded and BasePrice"
}

variable "max_gb" {
  description = "The max size of the database in gigabytes"
}

variable "read_scale" {
  description = "If enabled, connections that have application intent set to readonly in their connection string may be routed to a readonly secondary replica. This property is only settable for Premium and Business Critical databases."
}

variable "sku" {
  description = "Specifies the name of the SKU used by the database. For example, GP_S_Gen5_2,HS_Gen4_1,BC_Gen5_2, ElasticPool, Basic,S0, P2 ,DW100c, DS100. Changing this from the HyperScale service tier to another service tier will create a new resource."
}

variable "tags" {
  description = "Resource tags to be applied throughout the deployment."
  default     = {}
}

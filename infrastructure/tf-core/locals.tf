locals {
  primary_region = [for k, v in var.regions : k if v.is_primary_region][0]

  ## Overrides mainly used by dynamically created Temporary environments:
  sql_admin_group_name = var.sqlserver.sql_admin_group_name != "" ? var.sqlserver.sql_admin_group_name : "sqlsvr_${var.application}_${lower(var.environment)}_${local.primary_region}_admin"

}

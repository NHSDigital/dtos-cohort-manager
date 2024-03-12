module "azuresql" {
  source = ".//modules/arm-azure-sql-server"

  depends_on = [
    module.key_vault
  ]

  names               = module.config.names
  resource_group_name = module.baseline.resource_groups[var.sqlserver.resource_group_index].name
  location            = module.baseline.resource_groups[var.sqlserver.resource_group_index].location
  sqlversion          = var.sqlserver.sqlversion
  tlsver              = var.sqlserver.tlsversion

  tags = var.tags

}

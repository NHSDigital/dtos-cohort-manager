module "azuresql" {
  source = ".//modules/azure-sql-server"

  # Azure SQL Server
  names               = module.config.names
  resource_group_name = module.baseline.resource_group_names[var.sqlserver.resource_group_key]
  location            = module.baseline.resource_group_locations[var.sqlserver.resource_group_key]
  sqlversion          = var.sqlserver.sqlversion
  tlsver              = var.sqlserver.tlsversion
  kv_id               = module.key_vault.key_vault_id

  tags = var.tags

  # Default database
  db_name_suffix = var.sqlserver.db_name_suffix
  collation      = var.sqlserver.collation
  licence_type   = var.sqlserver.licence_type
  max_gb         = var.sqlserver.max_gb
  read_scale     = var.sqlserver.read_scale
  sku            = var.sqlserver.sku

  # FW Rules
  fw_rule_name = var.sqlserver.fw_rule_name
  start_ip     = var.sqlserver.start_ip
  end_ip       = var.sqlserver.end_ip
}


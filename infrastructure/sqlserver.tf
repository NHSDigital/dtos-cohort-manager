module "azuresql" {
  source = ".//modules/azure-sql-server"

  # Azure SQL Server
  names               = module.config.names
  resource_group_name = module.baseline.resource_group_names[var.sqlserver.server.resource_group_key]
  location            = module.baseline.resource_group_locations[var.sqlserver.server.resource_group_key]
  sqlversion          = var.sqlserver.server.sqlversion
  tlsver              = var.sqlserver.server.tlsversion
  kv_id               = module.key_vault.key_vault_id

  tags = var.tags

  # Default database
  db_name_suffix = var.sqlserver.dbs.cohman.db_name_suffix
  collation      = var.sqlserver.dbs.cohman.collation
  licence_type   = var.sqlserver.dbs.cohman.licence_type
  max_gb         = var.sqlserver.dbs.cohman.max_gb
  read_scale     = var.sqlserver.dbs.cohman.read_scale
  sku            = var.sqlserver.dbs.cohman.sku

  # FW Rules
  azurepassthrough = var.sqlserver.server.azure_services_access_enabled
  fw_rule_name     = var.sqlserver.fw_rules.passthrough.fw_rule_name
  start_ip         = var.sqlserver.fw_rules.passthrough.start_ip
  end_ip           = var.sqlserver.fw_rules.passthrough.end_ip
}


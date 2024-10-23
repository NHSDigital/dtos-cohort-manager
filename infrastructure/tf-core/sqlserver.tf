module "azure_sql_server" {
  for_each = {
    for key, value in var.regions : key => value
    if var.sqlserver != {}
  }

  #source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/azure-sql-server?ref=6dbb0d4f42e3fd1f94d4b8e85ef596b7d01844bc"
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/sql-server?ref=3ff9a88bed4d9506e516809bdb9c5757b362ac2d"

  # Azure SQL Server
  name                = module.regions_config[each.key].names.sql-server
  resource_group_name = module.baseline.resource_group_names[var.sqlserver.server.resource_group_key]
  location            = module.baseline.resource_group_locations[var.sqlserver.server.resource_group_key]

  sqlversion = var.sqlserver.server.sqlversion
  tlsver     = var.sqlserver.server.tlsversion
  kv_id      = module.key_vault[each.key].key_vault_id

  sql_uai_name         = var.sqlserver.sql_uai_name
  sql_admin_group_name = var.sqlserver.sql_admin_group_name
  sql_admin_object_id  = data.azuread_group.sql_admin_group.object_id
  ad_auth_only         = var.sqlserver.ad_auth_only

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

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids_sql             = [data.terraform_remote_state.hub.outputs.private_dns_zone_azure_sql[each.key].private_dns_zone.id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.key].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.key].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  tags = var.tags
}

data "azuread_group" "sql_admin_group" {
  display_name = var.sqlserver.sql_admin_group_name
}

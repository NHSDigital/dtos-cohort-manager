resource "azurerm_mssql_server" "azure_sql_server" {

  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  version             = var.sqlversion

  minimum_tls_version = var.tlsver

  tags = var.tags

  azuread_administrator {
    azuread_authentication_only = var.ad_auth_only         # set to: true
    login_username              = var.sql_admin_group_name # azurerm_user_assigned_identity.uai-sql.name
    object_id                   = var.sql_admin_object_id  # azurerm_user_assigned_identity.uai-sql.principal_id
  }

  lifecycle {
    ignore_changes = [tags]
  }
}

resource "azurerm_mssql_firewall_rule" "azurepassthrough" {

  count = var.azurepassthrough ? 1 : 0

  name             = var.fw_rule_name
  server_id        = azurerm_mssql_server.azure_sql_server.id
  start_ip_address = var.start_ip
  end_ip_address   = var.end_ip
}

/* --------------------------------------------------------------------------------------------------
  Private Endpoint Configuration for SQL Server
-------------------------------------------------------------------------------------------------- */

module "private_endpoint_sql_server" {
  count = var.private_endpoint_properties.private_endpoint_enabled ? 1 : 0

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/private-endpoint?ref=08100f7db2da6c0f64f327d15477a217a7ed4cd9"

  name                = "${var.name}-sql-private-endpoint"
  resource_group_name = var.private_endpoint_properties.private_endpoint_resource_group_name
  location            = var.location
  subnet_id           = var.private_endpoint_properties.private_endpoint_subnet_id

  private_dns_zone_group = {
    name                 = "${var.name}-sql-private-endpoint-zone-group"
    private_dns_zone_ids = var.private_endpoint_properties.private_dns_zone_ids_sql
  }

  private_service_connection = {
    name                           = "${var.name}-sql-private-endpoint-connection"
    private_connection_resource_id = azurerm_mssql_server.azure_sql_server.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = var.private_endpoint_properties.private_service_connection_is_manual
  }

  tags = var.tags
}

/* --------------------------------------------------------------------------------------------------
  Diagnostics Settings
-------------------------------------------------------------------------------------------------- */
resource "azurerm_mssql_server_extended_auditing_policy" "sqlserver_extended_auditing_policy" {
  server_id              = azurerm_mssql_server.azure_sql_server.id
  log_monitoring_enabled = var.sql_security_audit_logs_enabled
}

module "diagnostic-setting" {
  count = var.diagnostic_setting_properties.diagnostic_settings_globally_enabled ? 1 : 0

  source = "../diagnostic-setting"

  # Diagnostics setting parameters
  # log_categories                = var.diagnostic_setting_properties.log_categories
  
  name                          = "${var.name}-diagnostic_setting"
  resource_group_name           = var.resource_group_name
  location                      = var.location
  target_resource_id            = azurerm_mssql_server.azure_sql_server.id
  diagnostic_setting_properties = var.diagnostic_setting_properties
}
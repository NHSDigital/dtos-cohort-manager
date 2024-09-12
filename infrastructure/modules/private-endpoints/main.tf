resource "azurerm_private_dns_zone" "storage_private_dns_zone" {
  count               = var.is_storage_private_dns_zone_enabled ? 1 : 0
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = var.resource_group_name
}

resource "azurerm_private_dns_zone" "function_app_private_dns_zone" {
  count               = var.is_function_app_private_dns_zone_enabled ? 1 : 0
  name                = "privatelink.azurewebsites.net"
  resource_group_name = var.resource_group_name
}

resource "azurerm_private_dns_zone" "azure_sql_private_dns_zone" {
  count               = var.is_azure_sql_private_dns_zone_enabled ? 1 : 0
  name                = "privatelink.database.windows.net"
  resource_group_name = var.resource_group_name
}


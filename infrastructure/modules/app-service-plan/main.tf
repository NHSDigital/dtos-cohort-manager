resource "azurerm_service_plan" "appserviceplan" {

  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location

  os_type  = var.os_type
  sku_name = var.sku_name

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

resource "azurerm_app_service_virtual_network_swift_connection" "appservice_vnet_swift_connection" {
  count = var.vnet_integration_enabled ? 1 : 0

  app_service_id = azurerm_service_plan.appserviceplan.id
  subnet_id      = var.vnet_integration_subnet_id
}

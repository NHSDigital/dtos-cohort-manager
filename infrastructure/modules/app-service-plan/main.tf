resource "azurerm_service_plan" "appserviceplan" {

  name                = var.names.app-service-plan
  resource_group_name = var.resource_group_name
  location            = var.location

  os_type  = var.os_type
  sku_name = var.sku_name

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }

}

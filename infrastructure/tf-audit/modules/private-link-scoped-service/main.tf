resource "azurerm_monitor_private_link_scoped_service" "ampls_service" {
  name                = var.name
  resource_group_name = var.resource_group_name

  scope_name         = var.scope_name
  linked_resource_id = var.linked_resource_id
}

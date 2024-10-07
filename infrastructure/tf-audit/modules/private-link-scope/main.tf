resource "azurerm_monitor_private_link_scope" "ampls" {
  name                = var.name
  resource_group_name = var.resource_group_name

  ingestion_access_mode = var.ingestion_access_mode
  query_access_mode     = var.query_access_mode

  tags = var.tags
}

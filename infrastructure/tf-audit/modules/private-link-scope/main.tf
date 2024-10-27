resource "azurerm_monitor_private_link_scope" "ampls" {
  name                = var.name
  resource_group_name = var.resource_group_name

  ingestion_access_mode = var.ingestion_access_mode
  query_access_mode     = var.query_access_mode

  tags = var.tags
}

/* --------------------------------------------------------------------------------------------------
  Private Endpoint Configuration for Private Links
-------------------------------------------------------------------------------------------------- */

module "private_endpoint_ampls" {
  count = var.private_endpoint_properties.private_endpoint_enabled ? 1 : 0

  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/private-endpoint?ref=08100f7db2da6c0f64f327d15477a217a7ed4cd9"

  name                = "${var.name}-ampls-private-endpoint"
  resource_group_name = var.private_endpoint_properties.private_endpoint_resource_group_name
  location            = var.location
  subnet_id           = var.private_endpoint_properties.private_endpoint_subnet_id

  private_dns_zone_group = {
    name                 = "${var.name}-ampls-private-endpoint-zone-group"
    private_dns_zone_ids = var.private_endpoint_properties.private_dns_zone_ids
  }

  private_service_connection = {
    name                           = "${var.name}-ampls-private-endpoint-connection"
    private_connection_resource_id = azurerm_monitor_private_link_scope.ampls.id
    subresource_names              = ["azuremonitor"]
    is_manual_connection           = var.private_endpoint_properties.private_service_connection_is_manual
  }

  tags = var.tags
}

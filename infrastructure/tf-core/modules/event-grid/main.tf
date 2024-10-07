resource "azurerm_eventgrid_topic" "egtopic" {

  name                = "${var.names.event-grid-topic}-${var.name_suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

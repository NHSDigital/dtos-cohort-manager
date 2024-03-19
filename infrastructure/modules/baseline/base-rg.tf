resource "azurerm_resource_group" "rg" {

  for_each = { for g in var.resource_groups : g => g }
  name     = "${var.names.resource-group}-${each.value}"
  location = var.location
  tags     = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

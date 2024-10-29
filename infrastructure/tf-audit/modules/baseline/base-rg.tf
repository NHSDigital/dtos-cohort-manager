
resource "azurerm_resource_group" "rg-audit" {
  for_each = var.resource_groups

  name     = each.value.name
  location = each.value.location

  lifecycle {
    ignore_changes = [tags]
  }
}

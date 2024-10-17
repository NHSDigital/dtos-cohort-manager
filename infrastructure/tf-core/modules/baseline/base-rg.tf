
resource "azurerm_resource_group" "rg" {
  for_each = var.resource_groups

  provider = azurerm

  name     = each.value.name
  location = var.location

  lifecycle {
    ignore_changes = [tags]
  }
}

resource "azurerm_resource_group" "rg-audit" {
  for_each = var.resource_groups_audit

  provider = azurerm.audit

  name     = each.value.name
  location = each.value.location

  lifecycle {
    ignore_changes = [tags]
  }
}

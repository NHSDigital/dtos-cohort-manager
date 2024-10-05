resource "azurerm_storage_account" "storage_account" {

  name                          = var.name
  resource_group_name           = var.resource_group_name
  location                      = var.location

  account_replication_type      = var.account_replication_type
  account_tier                  = var.account_tier
  public_network_access_enabled = var.public_network_access_enabled

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

resource "azurerm_storage_container" "container" {
  for_each = var.containers

  name                  = each.value.container_name
  storage_account_name  = azurerm_storage_account.storage_account.name
  container_access_type = each.value.container_access_type
}

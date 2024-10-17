resource "azurerm_storage_account" "sa" {
  for_each = var.storage_accounts

  name                          = substr("${var.names.storage-account}${lower(each.value.name_suffix)}", 0, 24)
  resource_group_name           = var.resource_group_name
  location                      = var.location
  account_tier                  = each.value.account_tier
  account_replication_type      = each.value.replication_type
  public_network_access_enabled = each.value.public_network_access_enabled

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}


resource "azurerm_storage_container" "container" {
  for_each = var.containers

  name                  = each.value.cont_name
  storage_account_name  = azurerm_storage_account.sa[each.value.sa_key].name
  container_access_type = each.value.cont_access_type
}

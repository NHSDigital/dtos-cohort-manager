resource "azurerm_storage_account" "sa" {
  for_each = var.storage_accounts

  name                          = substr("${var.names.storage-account}${lower(each.value.name_suffix)}", 0, 24)
  resource_group_name           = var.resource_group_name
  location                      = var.location
  account_tier                  = each.value.account_tier
  account_replication_type      = each.value.replication_type
  public_network_access_enabled = each.value.public_access

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

resource "azurerm_storage_container" "container" {
  for_each = { for sa_key, sa_value in var.storage_accounts : sa_key => sa_value.containers if length(sa_value.containers) > 0 }

  count = length(each.value)

  name                  = each.value[count.index].cont_name
  storage_account_name  = azurerm_storage_account.sa[each.key].name
  container_access_type = each.value[count.index].cont_access_type
}

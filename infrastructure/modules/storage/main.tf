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
  for_each = {
    for account_key, account_data in var.storage_accounts : account_key => account_data.containers
    if length(account_data.containers) > 0 # Only iterate if containers are defined
  }

  name                  = each.value.cont_name
  storage_account_name  = azurerm_storage_account.sa[each.key].name
  container_access_type = each.value.cont_access_type
}

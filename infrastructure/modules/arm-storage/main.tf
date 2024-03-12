resource "azurerm_storage_account" "sa" {

  name                          = var.name
  resource_group_name           = var.resource_group_name
  location                      = var.location
  account_tier                  = var.account_tier
  account_replication_type      = var.sa_replication_type
  public_network_access_enabled = var.public_access

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

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

resource "azurerm_storage_account" "fileexception" {

  name                          = var.fe_name
  resource_group_name           = var.fe_resource_group_name
  location                      = var.fe_location
  account_tier                  = var.fe_account_tier
  account_replication_type      = var.fe_sa_replication_type
  public_network_access_enabled = var.fe_public_access

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

resource "azurerm_storage_container" "example" {
  name                  = var.fe_cont_name # "vhds"
  storage_account_name  = azurerm_storage_account.fileexception.name
  container_access_type = var.fe_cont_access_type # "private"
}

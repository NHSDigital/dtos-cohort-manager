data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "keyvault" {
  name                        = var.names.key-vault
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = var.disk_encryption
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = var.soft_delete_retention
  purge_protection_enabled    = var.purge_protection_enabled
  sku_name                    = var.sku_name

  public_network_access_enabled = true

  access_policy {

    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    certificate_permissions = ["Get", "List"]
    key_permissions         = ["Get", "List"]
    secret_permissions      = ["Get", "Set", "List"]
    storage_permissions     = ["Get", "List"]
  }

  tags = var.tags

  lifecycle {
    ignore_changes = [tags, contact]
  }
}


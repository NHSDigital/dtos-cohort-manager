data "azurerm_key_vault" "keyvault" {
  name                = var.names.key-vault
  resource_group_name = var.resource_group_name
}


resource "azurerm_key_vault_secret" "sqllogin" {
  name         = "az-sql-login"
  value        = "sqldtosadmin"
  key_vault_id = data.azurerm_key_vault.keyvault.id

  lifecycle {
    ignore_changes = [tags]
  }
}

## Random administrator password
resource "random_password" "randompass" {
  length  = 16
  special = true
}

resource "azurerm_key_vault_secret" "sqlpass" {
  name         = "az-sql-pass"
  value        = random_password.randompass.result
  key_vault_id = data.azurerm_key_vault.keyvault.id

  lifecycle {
    ignore_changes = [tags]
  }
}

resource "azurerm_mssql_server" "sqlserver" {
  name                         = var.names.sql-server
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = var.sqlversion
  administrator_login          = azurerm_key_vault_secret.sqllogin.value
  administrator_login_password = azurerm_key_vault_secret.sqlpass.value
  minimum_tls_version          = var.tlsver

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
  }
}

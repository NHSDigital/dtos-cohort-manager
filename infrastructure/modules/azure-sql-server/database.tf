
resource "azurerm_mssql_database" "defaultdb" {
  name         = var.db_name_suffix # using only the suffix, as this is the naming convention from DToS Devs
  server_id    = azurerm_mssql_server.sqlserver.id
  collation    = var.collation
  license_type = var.licence_type
  max_size_gb  = var.max_gb
  read_scale   = var.read_scale
  sku_name     = var.sku

  tags = var.tags

  lifecycle {
    ignore_changes = [tags]
    # prevent the possibility of accidental data loss (variables may not be used here)
    prevent_destroy = false
  }
}

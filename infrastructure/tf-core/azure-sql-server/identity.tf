
# resource "azurerm_user_assigned_identity" "uai-sql" {
#   location            = var.location
#   resource_group_name = var.resource_group_name
#   name                = var.sql_uai_name
# }

# resource "azurerm_role_assignment" "ra" {
#   scope                = azurerm_mssql_server.sqlserver.id
#   role_definition_name = "SQL DB Contributor"
#   principal_id         = azurerm_user_assigned_identity.uai-sql.principal_id
# }

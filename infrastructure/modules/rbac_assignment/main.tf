# Note it is not necessary to declare the name for the role assignment as this is auto-generated in GUID format.
resource "azurerm_role_assignment" "role_assignment" {
  scope              = var.scope
  principal_id       = var.principal_id
  role_definition_id = data.azurerm_role_definition.role_definition.id
}

# Look up the role definition by name as a convenience for the user
data "azurerm_role_definition" "role_definition" {
  name = var.role_definition_name
}


resource "azurerm_user_assigned_identity" "this" {
  name                = var.uai_name
  resource_group_name = var.resource_group_name
  location            = var.location

  tags = var.tags
}

# create role assignment if required
resource "azurerm_role_assignment" "this" {
  # only create if role_assignment_resource_id is provided
  count = var.role_assignment_resource_id != "" ? 1 : 0

  scope                = var.role_assignment_resource_id
  role_definition_name = var.role_definition_name
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

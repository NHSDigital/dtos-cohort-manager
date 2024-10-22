
resource "azurerm_application_insights" "appins_audit" {

  name                = var.name
  location            = var.location
  resource_group_name = var.audit_resource_group_name
  workspace_id        = var.audit_law_id
  application_type    = var.appinsights_type

  tags = var.tags
}

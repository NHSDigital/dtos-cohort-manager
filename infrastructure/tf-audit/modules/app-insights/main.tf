
resource "azurerm_application_insights" "appins" {

  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  workspace_id        = var.log_analytics_workspace_id
  application_type    = var.appinsights_type

  tags = var.tags
}

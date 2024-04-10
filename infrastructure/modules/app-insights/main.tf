resource "azurerm_application_insights" "appins" {

  name                = "${var.names.app-insights}-${upper(var.name_suffix)}-${upper(var.appinsights_type)}"
  location            = var.location
  resource_group_name = var.resource_group_name
  workspace_id        = var.law_id
  application_type    = var.appinsights_type

  tags = var.tags
}

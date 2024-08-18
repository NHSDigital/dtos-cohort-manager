resource "azurerm_application_insights" "appins" {
  provider = azurerm

  name                = "${var.names.app-insights}-${upper(var.name_suffix)}-${upper(var.appinsights_type)}"
  location            = var.location
  resource_group_name = var.resource_group_name
  workspace_id        = var.law_id
  application_type    = var.appinsights_type

  tags = var.tags
}

resource "azurerm_application_insights" "appins_audit" {
  provider = azurerm.audit

  name                = "${var.names.app-insights}-${upper(var.name_suffix)}-${upper(var.appinsights_type)}"
  location            = var.location
  resource_group_name = var.resource_group_name
  workspace_id        = var.audit_law_id
  application_type    = var.appinsights_type

  tags = var.tags
}

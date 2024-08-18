resource "azurerm_log_analytics_workspace" "law" {
  provider = azurerm.default

  name                = "${var.names.log-analytics-workspace}-${upper(var.name_suffix)}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.law_sku
  retention_in_days   = var.retention_days

  tags = var.tags
}

resource "azurerm_log_analytics_workspace" "law_audit" {
  provider = azurerm.audit

  name                = "${var.names.log-analytics-workspace}-${upper(var.name_suffix)}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.law_sku
  retention_in_days   = var.retention_days

  tags = var.tags
}

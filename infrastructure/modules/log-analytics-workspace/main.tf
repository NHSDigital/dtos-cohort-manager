resource "azurerm_log_analytics_workspace" "law" {

  name                = "${var.names.log-analytics-workspace}-${upper(var.name_suffix)}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.law_sku
  retention_in_days   = var.retention_days

  tags = var.tags
}

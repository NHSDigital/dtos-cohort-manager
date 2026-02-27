locals {
  avail = var.availability_test
}

module "azurerm_application_insights_standard_availability_test" {
  for_each = { for key, val in var.regions : key => val if val.is_primary_region && var.features.alerts_enabled && local.avail != null }

  source                  = "../../../dtos-devops-templates/infrastructure/modules/application-insights-availability-test"
  name                    = "${module.regions_config[each.key].names.function-app}-${var.availability_test.name_suffix}"
  resource_group_name     = data.azurerm_application_insights.ai.resource_group_name
  location                = each.key
  action_group_id         = var.features.alerts_enabled ? module.monitor_action_group_performance[0].monitor_action_group.id : null
  application_insights_id = data.azurerm_application_insights.ai.id

  target_url = var.availability_test.target_url

  timeout   = var.availability_test.timeout
  frequency = var.availability_test.frequency

  headers = {
    OCP-Apim-Subscription-Key = var.OCP_APIM_SUBSCRIPTION_KEY
  }

  ssl_validation = {
    expected_status_code        = var.availability_test.ssl_validation.expected_status_code
    ssl_cert_remaining_lifetime = var.availability_test.ssl_validation.ssl_cert_remaining_lifetime
  }

  alert = var.availability_test.alert
}

resource "azurerm_resource_group" "monitoring" {
  name     = "${module.regions_config[local.primary_region].names.resource-group}-monitoring"
  location = local.primary_region

  tags = local.merged_tags
}

module "monitor_action_group" {
  for_each = var.monitor_action_groups

  source = "../../../dtos-devops-templates/infrastructure/modules/monitor-action-group"

  name                = "${module.regions_config[local.primary_region].names.monitor-action-group}-${each.key}"
  resource_group_name = azurerm_resource_group.monitoring.name
  short_name          = each.value.short_name
  email_receiver      = each.value.email_receiver
  event_hub_receiver  = each.value.event_hub_receiver
  sms_receiver        = each.value.sms_receiver
  voice_receiver      = each.value.voice_receiver
  webhook_receiver    = each.value.webhook_receiver
}


resource "azurerm_monitor_autoscale_setting" "asp_autoscale" {
  name                = "${var.names.app-service-plan}-autoscale"
  resource_group_name = var.resource_group_name
  location            = var.location
  target_resource_id  = azurerm_service_plan.appserviceplan.id

  profile {
    name = "${var.names.app-service-plan}-${var.metric}"

    capacity {
      minimum = var.capacity_min
      maximum = var.capacity_max
      default = var.capacity_def
    }

    rule {
      metric_trigger {
        metric_name        = var.metric
        metric_resource_id = azurerm_service_plan.appserviceplan.id
        time_grain         = var.time_grain
        statistic          = var.statistic
        time_window        = var.time_window
        time_aggregation   = var.time_aggregation
        operator           = var.inc_operator
        threshold          = var.inc_threshold
      }

      scale_action {
        direction = var.inc_scale_direction
        type      = var.inc_scale_type
        value     = var.inc_scale_value
        cooldown  = var.inc_scale_cooldown
      }
    }

    rule {
      metric_trigger {
        metric_name        = var.metric
        metric_resource_id = azurerm_service_plan.appserviceplan.id
        time_grain         = var.time_grain
        statistic          = var.statistic
        time_window        = var.time_window
        time_aggregation   = var.time_aggregation
        operator           = var.dec_operator
        threshold          = var.dec_threshold
      }

      scale_action {
        direction = var.dec_scale_direction
        type      = var.dec_scale_type
        value     = var.dec_scale_value
        cooldown  = var.dec_scale_cooldown
      }
    }
  }
}

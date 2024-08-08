
resource "azurerm_linux_function_app" "function" {
  for_each = var.function_app

  name                = "${var.names.function-app}-${lower(each.value.name_suffix)}"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = var.asp_id

  storage_account_name       = var.sa_name
  storage_account_access_key = var.sa_prm_key

  site_config {

    container_registry_use_managed_identity       = var.cont_registry_use_mi
    container_registry_managed_identity_client_id = var.acr_mi_client_id

    application_insights_connection_string = var.ai_connstring
    use_32_bit_worker                      = var.gl_worker_32bit

    application_stack {
      docker {
        registry_url = var.acr_registry_url
        image_name   = "${var.docker_img_prefix}-${lower(each.value.name_suffix)}"
        image_tag    = var.image_tag
      }
    }
  }

  identity {
    type         = "SystemAssigned, UserAssigned"
    identity_ids = [var.acr_mi_id]
  }

  app_settings = merge(local.global_app_settings, local.app_settings[each.key])

  tags = var.tags

  lifecycle {
    #ignore_changes = [tags, app_settings, connection_string]
    ignore_changes = [tags, connection_string]
  }

}

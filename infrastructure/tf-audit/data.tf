data "azurerm_client_config" "current" {}

data "terraform_remote_state" "hub" {
  backend = "azurerm"
  config = {
    subscription_id      = var.HUB_SUBSCRIPTION_ID
    storage_account_name = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_NAME
    container_name       = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME
    key                  = var.HUB_BACKEND_AZURE_STORAGE_ACCOUNT_KEY
    resource_group_name  = var.HUB_BACKEND_AZURE_RESOURCE_GROUP_NAME
  }
}

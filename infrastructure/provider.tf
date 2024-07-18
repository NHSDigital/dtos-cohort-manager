terraform {
  required_version = ">= 1.9.2"
  required_providers {
    azurerm = "= 3.69.0"
    random  = "~> 3.5.1"
  }
}

provider "azurerm" {
  features {}
  # Subscription Id to create the resources in passed in via TF variables
  subscription_id = var.TARGET_SUBSCRIPTION_ID
}

module "config" {
  source      = ".//modules/shared-config"
  location    = var.location
  application = var.application
  env         = var.environment
  tags        = var.tags
}

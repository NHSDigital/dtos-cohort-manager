terraform {
  required_version = ">= 1.4.2"
  required_providers {
    azurerm = "= 3.69.0"
    random  = "~> 3.5.1"
  }
}

provider "azurerm" {

  features {}
}

module "config" {
  source      = ".//modules/shared-config"
  location    = var.location
  application = var.application
  env         = var.environment
  tags        = var.tags
}

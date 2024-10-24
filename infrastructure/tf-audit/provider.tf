terraform {
  required_version = ">= 1.9.2"
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      # version = ">= 4.2.0"
      version = "= 3.112.0"
    }

    random = "~> 3.5.1"
    azuread = {
      source  = "hashicorp/azuread"
      version = "2.53.1"
    }
  }
}

provider "azurerm" {
  subscription_id = var.AUDIT_SUBSCRIPTION_ID
  # Configuration options

  features {}
}

provider "azurerm" {
  alias           = "acr_subscription"
  subscription_id = var.ACR_SUBSCRIPTION_ID
  # Configuration options

  features {}
}

provider "azurerm" {
  alias           = "hub"
  subscription_id = var.HUB_SUBSCRIPTION_ID
  features {}
}

provider "azuread" {
  # Configuration options
}

module "config" {
  source = ".//modules/shared-config"

  location              = var.location
  application           = var.application
  application_full_name = var.application_full_name
  env                   = var.environment
  tags                  = var.tags
}

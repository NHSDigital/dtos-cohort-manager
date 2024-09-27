terraform {
  required_version = ">= 1.9.2"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
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
  features {}
  # Subscription Id to create the resources in passed in via TF variables
  subscription_id = var.TARGET_SUBSCRIPTION_ID
}

provider "azurerm" {
  alias           = "audit"
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
  alias           = "devops"
  subscription_id = var.DEVOPS_SUBSCRIPTION_ID
  features {}
}

provider "azuread" {
  # Configuration options
}

module "config" {
  source      = ".//modules/shared-config"
  location    = var.location
  application = var.application
  env         = var.environment
  tags        = var.tags
}

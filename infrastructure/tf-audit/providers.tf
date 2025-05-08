terraform {
  backend "azurerm" {}
  required_version = ">= 1.9.2"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "4.26"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "2.53.1"
    }
    random = "~> 3.5.1"
  }
}

provider "azurerm" {
  subscription_id = var.TARGET_SUBSCRIPTION_ID
  features {}
}

provider "azurerm" {
  alias           = "hub"
  subscription_id = var.HUB_SUBSCRIPTION_ID
  features {}
}

provider "azuread" {}

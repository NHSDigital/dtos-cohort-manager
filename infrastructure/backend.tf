
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-dtos-tfstate"
    storage_account_name = "sadtostfstate9584"
    container_name       = "tfstatebaseline"
    key                  = "baseline.terraform.tfstate"
  }
}

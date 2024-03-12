
variable "application" {}
variable "environment" {}
variable "location" {}
variable "tags" { default = {} }

variable "resource_groups" {}
variable "storage_accounts" {}
variable "key_vault" { default = {} }
variable "sqlserver" {}
variable "app_service_plan" {}

variable "function_app" {}

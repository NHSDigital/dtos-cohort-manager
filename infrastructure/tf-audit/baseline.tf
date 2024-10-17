module "baseline" {
  source = "modules/baseline"

  providers = {
    azurerm       = azurerm
    azurerm.audit = azurerm.audit
  }

  location        = var.location
  names           = module.config.names
  tags            = var.tags
  resource_groups = var.resource_groups

  resource_groups_audit = var.resource_groups_audit

}

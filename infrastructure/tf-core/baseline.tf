module "baseline" {
  source = ".//modules/baseline"

  providers = {
    azurerm = azurerm
  }

  location        = var.location
  names           = module.config.names
  tags            = var.tags
  resource_groups = var.resource_groups

}

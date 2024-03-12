module "baseline" {
  source = ".//modules/arm-baseline"

  location        = var.location
  names           = module.config.names
  tags            = var.tags
  resource_groups = var.resource_groups

}

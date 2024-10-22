module "baseline" {
  source = ".//modules/baseline"

  location = var.location
  names    = module.config.names
  tags     = var.tags

  resource_groups_audit = var.resource_groups_audit

}

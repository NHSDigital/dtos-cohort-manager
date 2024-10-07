module "regions_config" {
  for_each = var.regions

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/shared-config?ref=36101b5776ad52fb0909a6e17fd3fb9b6c6db540"

  location    = each.key
  application = var.application
  env         = var.environment
  tags        = var.tags
}

module "config" {
  source                = ".//modules/shared-config"
  location              = var.location
  application           = var.application
  application_full_name = var.application_full_name
  env                   = var.environment
  tags                  = var.tags
}

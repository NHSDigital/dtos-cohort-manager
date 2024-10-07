module "regions_config" {
  for_each = var.regions

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/shared-config?ref=e125d928afd9546e06d8af9bdb6391cbf6336773"

  location    = each.key
  application = var.application
  env         = var.environment
  tags        = var.tags
}

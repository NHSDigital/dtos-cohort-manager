module "regions_config" {
  for_each = var.regions

  # Source location updated to use the git:: prefix to avoid URL encoding issues - note // between the URL and the path is required
  source = "git::https://github.com/NHSDigital/dtos-devops-templates.git//infrastructure/modules/shared-config?ref=2296f761f4edc3b413e2629c98309df9c6fa0849"

  location    = each.key
  application = var.application
  env         = var.environment
  tags        = var.tags
}

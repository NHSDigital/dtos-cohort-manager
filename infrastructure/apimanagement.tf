module "api_management" {
  source = ".//modules/api-management"

  names = module.config.names

  resource_group_name = module.baseline.resource_group_names[var.api_management.resource_group_key]
  location            = module.baseline.resource_group_locations[var.api_management.resource_group_key]

  publisher_name  = var.api_management.publisher_name
  publisher_email = var.api_management.publisher_email
  sku             = var.api_management.sku

  tags = var.tags

}

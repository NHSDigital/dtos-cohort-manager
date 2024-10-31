module "baseline" {
  source = ".//modules/baseline"

  location = local.primary_region
  names    = module.regions_config[local.primary_region].names

  # This is an interim solution before we make resource groups fully regionalised
  resource_groups = {
    for resource_group_key, resource_group in var.resource_groups :
    resource_group_key => {
      name     = "${module.regions_config[local.primary_region].names.resource-group}${resource_group.name_suffix != "" ? "-${resource_group.name_suffix}" : ""}"
      location = local.primary_region
    }
  }

  tags = var.tags

}

locals {
  # Get the primary region from the regions map:
  primary_region_map = {
    for region_key, region in var.regions :
    region_key => region
    if region.is_primary_region
  }

  primary_region = keys(local.primary_region_map)[0]
}

output "primary_region" {
  value = local.primary_region
}

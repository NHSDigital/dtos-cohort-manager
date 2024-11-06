locals {
#   #   Get the primary region from the regions map:
  primary_region_map = { for region_key, region in var.regions : region_key => region if region.is_primary_region }
  primary_region = keys(local.primary_region_map)[0]
}

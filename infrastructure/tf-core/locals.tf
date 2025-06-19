locals {
  primary_region = [for k, v in var.regions : k if v.is_primary_region][0]
}

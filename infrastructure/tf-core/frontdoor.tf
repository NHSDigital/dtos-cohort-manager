module "frontdoor" {
  source = "../../../dtos-devops-templates/infrastructure/modules/cdn-frontdoor"

  providers = {
    azurerm = azurerm.hub
  }

  cdn_frontdoor_profile_id = data.terraform_remote_state.hub.outputs.cdn_frontdoor_profile["dtos-${var.application_full_name}"].id
  endpoint                 = var.frontdoor.endpoint
  origin_group             = var.frontdoor.origin_group
  origin                   = local.origin_map
  resource_group_name      = data.terraform_remote_state.hub.outputs.project_rg_names["dtos-${var.application_full_name}-${local.primary_region}"]
  route                    = local.route_map

  tags = var.tags
}

locals = {
  # Dynamically fetch the regional origins for the specified Web Apps. This needs to be dynamic to get the private_link_target_id values
  # There may be multiple origins and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  origins_object_list = flatten([
    for region in keys(var.regions) : [
      for origin, config in var.frontdoor.origin : merge(
        {
          region       = region # 1st iterator
          origin       = origin # 2nd iterator
          hostname     = "${module.regions_config[region].names["linux-web-app"]}-${var.linux_web_app.linux_web_app_config[origin].name_suffix}.azurewebsites.net"
          private_link = var.features.private_endpoints_enabled ? {
            # At time of writing (Jun 2025) Private Link connection requests must be approved manually in Portal, see note here:
            # https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/cdn_frontdoor_origin
            target_type            = "sites"
            location               = region
            private_link_target_id = module.linux_web_app.private_endpoint_id
          } : null
        },
        config # the rest of the key/value pairs for a specific origin
      }
    }
  }
  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  origin_map = {
    for object in local.origins_object_list : "${object.origin}-${object.region}" => object
  }

  # Populate cdn_frontdoor_origin_keys from the dynamic origins interpolated above.
  route_map = {
    for route, config in var.frontdoor.route : route => merge(
      {
        for k, v in config : k => v if k != "cdn_frontdoor_origin_key"
      },
      {
        cdn_frontdoor_origin_keys = [ for k, v in local.origin_map : k if v.origin == config.cdn_frontdoor_origin_key ]
      }
    )
  }
}

module "frontdoor_endpoint" {
  source = "../../../dtos-devops-templates/infrastructure/modules/cdn-frontdoor-endpoint"

  for_each = var.frontdoor_endpoint

  providers = {
    azurerm     = azurerm.hub # Each project's Front Door profile (with secrets) resides in Hub since it's shared infra with a Non-live/Live deployment pattern
    azurerm.dns = azurerm.hub
  }

  cdn_frontdoor_profile_id = data.terraform_remote_state.hub.outputs.frontdoor_profile["dtos-${var.application_full_name}"].id
  custom_domains = {
    for k, v in each.value.custom_domains : k => merge(
      v,
      {
        tls = merge(
          v.tls,
          v.tls.certificate_type == "CustomerCertificate" && v.tls.cdn_frontdoor_secret_key != null ? {
            cdn_frontdoor_secret_id = data.terraform_remote_state.hub.outputs.frontdoor_profile["dtos-${var.application_full_name}"].secrets[v.tls.cdn_frontdoor_secret_key].id
          } : {}
        )
      }
    )
  }
  name         = lower("${var.environment}-${each.key}")
  origin_group = each.value.origin_group
  origins = {
    for region in keys(var.regions) : module.linux_web_app["${each.value.origin.webapp_key}-${region}"].name => merge(
      each.value.origin,
      {
        hostname           = "${module.linux_web_app["${each.value.origin.webapp_key}-${region}"].name}.azurewebsites.net"
        origin_host_header = "${module.linux_web_app["${each.value.origin.webapp_key}-${region}"].name}.azurewebsites.net"
        private_link = var.features.private_endpoints_enabled ? {
          target_type            = "sites"
          location               = region
          private_link_target_id = module.linux_web_app["${each.value.origin.webapp_key}-${region}"].id
        } : null
      }
    )
  }
  route             = each.value.route
  security_policies = each.value.security_policies

  tags = var.tags
}

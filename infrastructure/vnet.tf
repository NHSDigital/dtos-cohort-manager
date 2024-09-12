module "vnet" {
  for_each = var.regions

  source = ".//modules/vnet"

  name                = "${var.environment}-vnet-${each.value.caf_short_name}"
  vnet_address_space  = each.value.vnet_address_space
  resource_group_name = module.baseline.resource_group_names[var.vnet.resource_group_key]
  location            = each.key

  tags = var.tags

}

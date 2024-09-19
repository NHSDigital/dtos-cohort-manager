
# Use this output to get anny other attribute of the vnet in the form:
# module.vnet_demo.vnet.<attribute>
output "vnet" {
  value = azurerm_virtual_network.vnet
}

output "name" {
  value = azurerm_virtual_network.vnet.name
}

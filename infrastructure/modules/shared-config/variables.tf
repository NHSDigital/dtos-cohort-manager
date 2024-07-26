variable "env" {
  description = "Environment acronym for deployment"
}

variable "location" {
  description = "Location for the deployment"
}

variable "location_map" {
  description = "Azure location map used for naming abberviations"
  type        = map(string)
  default = {
    "Australia Central 2"  = "CAU2",
    "Australia Central"    = "CAU",
    "Australia East"       = "EAU",
    "Australia Southeast"  = "SEAU",
    "australiacentral"     = "CAU",
    "australiacentral2"    = "CAU2",
    "australiaeast"        = "EAU",
    "australiasoutheast"   = "SEAU",
    "Brazil South"         = "SBR",
    "brazilsouth"          = "SBR",
    "Canada Central"       = "CAC",
    "Canada East"          = "ECA",
    "canadacentral"        = "CAC",
    "canadaeast"           = "ECA",
    "Central India"        = "CIN",
    "Central US"           = "CUS",
    "centralindia"         = "CIN",
    "centralus"            = "CUS",
    "East Asia"            = "EAA",
    "East US 2"            = "EUS2",
    "East US"              = "EUS",
    "eastasia"             = "EAA",
    "eastus"               = "EUS",
    "eastus2"              = "EUS2",
    "France Central"       = "CFR",
    "France South"         = "SFR",
    "francecentral"        = "CFR",
    "francesouth"          = "SFR",
    "Germany North"        = "NGE",
    "Germany West Central" = "WCGE",
    "germanynorth"         = "NGE",
    "germanywestcentral"   = "WCGE",
    "Japan East"           = "EJA",
    "Japan West"           = "WJA",
    "japaneast"            = "EJA",
    "japanwest"            = "WJA",
    "Korea Central"        = "CKO",
    "Korea South"          = "SKO",
    "koreacentral"         = "CKO",
    "koreasouth"           = "SKO",
    "North Central US"     = "NCUS",
    "North Europe"         = "NEU",
    "northcentralus"       = "NCUS",
    "northeurope"          = "NEU",
    "South Africa North"   = "NSA",
    "South Africa West"    = "WSA",
    "South Central US"     = "SCUS",
    "South India"          = "SIN",
    "southafricanorth"     = "NSA",
    "southafricawest"      = "WSA",
    "southcentralus"       = "SCUS",
    "Southeast Asia"       = "SEA",
    "southeastasia"        = "SEA",
    "southindia"           = "SIN",
    "UAE Central"          = "CUA",
    "UAE North"            = "NUA",
    "uaecentral"           = "CUA",
    "uaenorth"             = "NUA",
    "UK South"             = "UKS",
    "UK West"              = "WUK",
    "uksouth"              = "UKS",
    "ukwest"               = "WUK",
    "West Central US"      = "WCUS",
    "West Europe"          = "WEU",
    "West India"           = "WIN",
    "West US 2"            = "WUS2",
    "West US"              = "WUS",
    "westcentralus"        = "WCUS",
    "westeurope"           = "WEU",
    "westindia"            = "WIN",
    "westus"               = "WUS",
    "westus2"              = "WUS2"
  }
}

variable "application" {
  description = "Unique identifier for the deployment"
}

variable "tags" {
  type        = map(string)
  description = "Default tags for the deployment"
}

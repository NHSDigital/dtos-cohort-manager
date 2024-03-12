variable "location" {}
variable "names" {}
variable "tags" {
  default = {}
}

variable "resource_groups" {
  type = list(string)
}

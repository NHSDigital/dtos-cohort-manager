variable "location" {}
variable "names" {
  type = map(string)
}
variable "tags" {
  default = {}
}

variable "resource_groups" {
  type = list(string)
}

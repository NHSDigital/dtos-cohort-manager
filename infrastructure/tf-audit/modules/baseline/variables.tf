variable "location" {}
variable "names" {
  type = map(string)
}
variable "tags" {
  default = {}
}

variable "resource_groups_audit" {
  type = map(object({
    name     = string
    location = string
  }))
}

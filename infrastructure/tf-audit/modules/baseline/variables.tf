variable "location" {
  type = string
}

variable "names" {
  type = map(any)
}

variable "tags" {
  default = {}
}

variable "resource_groups" {
  type = map(object({
    name     = string
    location = string
  }))
}

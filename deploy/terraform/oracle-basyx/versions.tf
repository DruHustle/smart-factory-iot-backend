terraform {
  required_version = ">= 1.5.7"

  required_providers {
    oci = {
      source  = "oracle/oci"
      version = "~> 7.0"
    }
  }
}

provider "oci" {
  auth                = var.oci_auth
  config_file_profile = var.oci_profile
  region              = var.region
}

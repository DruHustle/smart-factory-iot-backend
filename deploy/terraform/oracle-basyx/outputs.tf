output "instance_id" {
  description = "OCI instance OCID."
  value       = oci_core_instance.basyx.id
}

output "public_ip" {
  description = "Public IP used for SSH and the future Cloudflare Tunnel origin."
  value       = oci_core_instance.basyx.public_ip
}

output "ssh_command" {
  description = "Command for connecting after cloud-init completes."
  value       = "ssh ubuntu@${oci_core_instance.basyx.public_ip}"
}

output "ubuntu_image" {
  description = "Ubuntu image selected at apply time."
  value       = data.oci_core_images.ubuntu_arm64.images[0].display_name
}

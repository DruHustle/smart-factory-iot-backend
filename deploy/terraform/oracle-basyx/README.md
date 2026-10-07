# Oracle Always Free BaSyx host

This Terraform module creates one Ubuntu 24.04 ARM64 VM on the OCI Ampere A1 Flex shape, plus an isolated VCN, public subnet, and internet gateway. It installs Docker and Docker Compose through cloud-init.

The defaults request 2 OCPUs, 12 GB RAM, and a 50 GB boot volume. The variable validations cap a continuously running VM at Oracle's current monthly Always Free A1 allowance and cap its boot volume at the regional free-storage allowance. These checks cannot account for resources created outside this module, partial-month usage, future Oracle policy changes, provider classification, or A1 capacity; review Limits, Quotas and Usage plus the cost estimate before every apply.

Only SSH is allowed inbound, and it must be restricted to a specific CIDR. BaSyx ports 8081–8086 are deliberately not exposed. Production access should use an authenticated HTTPS reverse proxy or Cloudflare Tunnel.

## 1. Authenticate with OCI

The default configuration uses a temporary browser-authenticated session, so a permanent API key is not required:

```bash
oci session authenticate --region eu-frankfurt-1 --profile-name DEFAULT
```

Complete sign-in and MFA in the browser. If the session expires, run the same command again. Permanent API-key authentication is also supported by setting `oci_auth = "ApiKey"` after configuring `~/.oci/config`.

## 2. Create local variables

```bash
cd deploy/terraform/oracle-basyx
cp terraform.tfvars.example terraform.tfvars
curl -4 https://ifconfig.me
```

Edit `terraform.tfvars` with the tenancy OCID, compartment OCID, and the returned IP followed by `/32`. `terraform.tfvars`, state, and plans are ignored by Git.

The root compartment uses the tenancy OCID as its compartment OCID. If you create a dedicated compartment, use that compartment's OCID instead.

## 3. Review before creation

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=basyx.tfplan
```

Review the plan and the Oracle cost estimate. Do not apply if the Console shows a non-zero estimate or the shape is not marked **Always Free-eligible**.

## 4. Create and connect

```bash
terraform apply basyx.tfplan
terraform output -raw ssh_command
```

Cloud-init can take several minutes. After connecting:

```bash
cloud-init status --wait
docker compose version
```

The BaSyx Compose deployment and its Aiven password are intentionally separate from Terraform so the database credential is not written into Terraform state.

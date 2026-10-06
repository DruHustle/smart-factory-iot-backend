# AASX Import and Edge Configuration

## Create an asset

The dashboard exposes one Create Asset workflow with Quick Create and Import Package (.aasx). Both use the private DeviceService and common IDTA repository storage. The Node API requires an engineer/admin session, applies a 50 MB request limit, and keeps the DeviceService token server-side.

`POST /api/assets` accepts the validated JSON form DTO. `POST /api/assets/import` accepts multipart field `file`; DeviceService validates OPC package relationships, reads the AAS core JSON/XML without extracting untrusted ZIP paths, registers shell/submodel/Concept Description records, and stores the original package and attachments with the AASX File Server. Imports are limited to 25 shells, 50 MB compressed, and 250 MB expanded.

The parser uses the [official generated AAS Core 3.1 C# SDK](https://github.com/eclipse-aascw/aas-core3.1-csharp), the current official C# generated SDK reference. JSON is validated through a temporary copy with AAS 3.2 `createdAt`/`updatedAt` fields removed; the imported JSON retains its original fields and vendor extensions. XML namespaces for AAS 3.0 and 3.2 are normalized in memory to the 3.1 reader's namespace, 3.2 administration dates are removed for validation and restored, and the parsed XML model is serialized to JSON. This does not claim complete AAS 3.2 model, vendor extension, or IDTA template conformance. Set `AASX_TEST_CORPUS_DIR` to a licensed directory of vendor `.aasx` files when running backend tests; the corpus itself is not distributed in Git.

The IDTA AASX 3.2.0 specification archive includes `examples/IDTA-01005_Example.aasx`. Its XML core model uses the AAS 3.0 namespace and contains two shells, two submodels, and references to embedded PDF parts. Run the parser test project with that extracted `examples` directory in `AASX_TEST_CORPUS_DIR`; the corpus test checks the shell/submodel extraction and the normative example's file reference. Separate generated tests check nested semantic IDs and AAS 3.2 administrative-date preservation. The archive version describes the package format; it does not mean every embedded AAS model uses metamodel 3.2.

## Repository and registry configuration

The companion dashboard repository's `docker compose --profile aas up -d --wait` starts BaSyx Go 1.1.0 beside the backend: AAS Repository 8081, AAS Registry 8082, Submodel Repository 8083, Submodel Registry 8084, Concept Description Repository 8085, and AASX File Server 8086. Host ports bind to loopback only, the BaSyx database is persistent, and both Compose projects communicate through the private `smart-factory-iot-shared` network. Check each service's `/description` for its advertised API 3.2 profiles.

Configure DeviceService with the following endpoints and identity settings:

| Variable | Purpose |
|---|---|
| `AAS_REPOSITORY_URL` (`AAS_REPO_URL` alias) | AAS Repository API for shell records |
| `AAS_SUBMODEL_REPOSITORY_URL` | Submodel Repository API |
| `AAS_CONCEPT_DESCRIPTION_REPOSITORY_URL` | Concept Description Repository API |
| `AAS_REGISTRY_URL` | AAS Registry API |
| `AAS_SUBMODEL_REGISTRY_URL` | Submodel Registry API |
| `AAS_REPOSITORY_REGISTRY_INTEGRATION` | `true` when repository services perform descriptor auto-registration |
| `AAS_OIDC_DISCOVERY_URL` or `AAS_OIDC_TOKEN_URL` | OIDC discovery URL or direct OAuth token endpoint; direct URL takes precedence |
| `AAS_OIDC_SCOPE` | Optional OAuth client-credentials scope |
| `AAS_OIDC_CLIENT_ID`, `AAS_OIDC_CLIENT_SECRET` | OAuth client credentials used by DeviceService for remote services |
| `AASX_FILE_SERVER_URL` | AASX package and attachment service |
| `AAS_PROVISIONING_TOKEN` | Shared Node-to-DeviceService credential; keep it out of browser and edge configuration |
| `REDIS_URL` | Shared Redis Pub/Sub connection required in production for cross-replica AAS notifications |
| `AAS_ALLOW_UNAUTHENTICATED_LOCAL` | Development-only opt-in; the service restricts it to known local hostnames and Development environment |
| `AAS_ALLOW_UNAUTHENTICATED_PRIVATE` | Explicit production opt-in for the bundled BaSyx service names; arbitrary or public hosts are rejected |
| `MqttBrokerHost`, `MqttBrokerPort`, `MqttUsername`, `MqttPassword`, `MqttUseTls` | CloudAMQP MQTT TLS for telemetry and gateway desired state; scope each backend identity and gateway topic ACL |

For a single-host production deployment without an identity provider, keep every BaSyx port bound to loopback/private Docker networks, set `AAS_ALLOW_UNAUTHENTICATED_PRIVATE=true`, and leave the OIDC variables empty. This override accepts only the bundled BaSyx service DNS names (and loopback); it rejects arbitrary remote hosts. Do not publish these unauthenticated APIs through a reverse proxy or public load balancer. Multi-host or publicly reachable production deployments should instead enable BaSyx OIDC and ABAC, use service-specific identities, and store credentials in a secret manager. OIDC discovery reads the provider's `token_endpoint`; the direct `AAS_OIDC_TOKEN_URL` setting overrides discovery, and `AAS_OIDC_SCOPE` is optional. Public IDTA demonstrators are for shared testing only.

Start the bundled stack from the companion dashboard repository with:

```bash
docker compose --profile aas up -d --build --wait
```

For production-mode DeviceService with the private stack, configure:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
AAS_ALLOW_UNAUTHENTICATED_LOCAL=false
AAS_ALLOW_UNAUTHENTICATED_PRIVATE=true
AAS_OIDC_TOKEN_URL=
AAS_OIDC_DISCOVERY_URL=
AAS_OIDC_SCOPE=
AAS_OIDC_CLIENT_ID=
AAS_OIDC_CLIENT_SECRET=
```

To validate the live BaSyx API profiles and exercise DeviceService form create, reads, registry auto-registration, duplicate rejection, revision update/conflict, and cleanup, start the root repository's `aas` and Redis Compose services and run:

```bash
python3 scripts/verify-basyx-live.py
```

With the same BaSyx stack running, `AAS_LIVE_TESTS=true dotnet test src/SmartFactory.sln --filter FullyQualifiedName~AasxLiveIntegrationTests` imports an AASX containing v3.2 timestamps and an embedded file, then downloads the stored package and verifies the attachment and model fields survived unchanged.

## Revision-aware form updates

`PUT /api/assets` requires `expectedAasVersion` and `aasVersion = expectedAasVersion + 1`. DeviceService reads current shell/submodel revisions before updating the repository and any separately managed descriptor. A stale edit returns HTTP 409. Prior remote JSON is retained during a multi-resource update; compensating PUTs are attempted if a later write fails. The dashboard stores append-only snapshots in PostgreSQL and serializes edits per asset, while the AAS runtime and PostgreSQL do not share a transaction. AASX imports retain vendor content and are not overwritten by the form editor.

## Edge desired state

When an asset is assigned to a live gateway, DeviceService publishes retained QoS 1 `replace_asset_configuration` desired state to `factory/{site}/{line}/{gateway}/commands`. The Python gateway validates the schema and gateway ID, atomically writes the profile with owner-only permissions, reloads its polling loop, and sends an acknowledgement on the topic's `/ack` child. Protocol passwords and private keys are not included. Broker ACLs should let each gateway subscribe only to its command topic and publish only telemetry, heartbeat, and acknowledgement topics. Broker publish success is not device acknowledgement; monitor the gateway acknowledgement.

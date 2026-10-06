# End-to-End Integration Checks

## Automated suites

The `SmartFactory.Tests` project covers telemetry parsing, persistence, event processing, and DeviceService authorization with isolated test dependencies.

```bash
dotnet test src/SmartFactory.sln -v minimal  # .NET 8 SDK and runtime required
```

The dashboard repository's `pnpm e2e` uses disposable PostgreSQL and Playwright for user roles, authentication, assets/AAS, API authorization, and telemetry bridge behavior. It does not use the developer's `.env.local` database.

## Full local flow

1. Configure local-only `.env.local` values and start the dashboard BaSyx/Redis stack first.
2. Start the backend with `docker compose --env-file .env.local up --build -d`. Share the session, ingestion, provisioning and dashboard-service tokens only with their intended callers; configure the current dashboard account database.
3. The supported Compose default is `http://dashboard:3000/api/internal/telemetry` on the shared network. Use `host.docker.internal` only when the API runs on the host. Production requires a private, encrypted service path.
4. Create a gateway and test asset, export its edge profile, and install it on a Pi with local protocol security settings.
5. Publish a normalized test message to `factory/{site}/{line}/{deviceId}/telemetry`; verify the MQTT ACL and TelemetryService logs.
6. Verify backend persistence and, when enabled, dashboard persistence with the separate service token. Confirm a wrong token is rejected.
7. Check role allow/deny behavior in DeviceService and the dashboard/AAS gateway.

Use a staging broker and synthetic/test machine values only. The timestamp is Unix epoch milliseconds UTC, not MCU uptime. See the companion edge README for Pi and ESP32 tests.


## Live AAS repository verification

The dashboard E2E uses an upstream stub for deterministic browser coverage. For live API 3.2 acceptance, start the root repository's BaSyx `aas` and Redis Compose profile, then run `python3 scripts/verify-basyx-live.py` here. It checks advertised repository/registry profiles and sends create/read/duplicate/revision-update/stale-update/delete requests through DeviceService. Run `AAS_LIVE_TESTS=true dotnet test src/SmartFactory.sln --filter FullyQualifiedName~AasxLiveIntegrationTests` to upload and download a package with v3.2 timestamps and an embedded file. Set `AASX_TEST_CORPUS_DIR` to licensed vendor packages when running `dotnet test`; the corpus is deliberately not committed. These checks are integration evidence and do not replace the official IDTA conformance/test engine for the exact deployment profile.

## Reproducible cross-repository test

From the dashboard repository, run `pnpm e2e:system` in a Python environment containing the edge requirements. This builds real DeviceService and TelemetryService containers and disposable databases, Redis, and MQTT. It uses the running local BaSyx stack, unique temporary AAS identities, and a simulated serial controller. It verifies profile publication/application, engineer control and viewer denial, named signal preservation, retry deduplication, dashboard outage recovery, and Assistant retrieval. It removes its containers, volumes and AAS records. It does not flash firmware, move hardware, validate cloud TLS/ACLs, or implement OTA.

## Combined Render image acceptance

The dashboard `scripts/bundle-smoke.py` runs the actual six-service image with isolated PostgreSQL, Redis and RabbitMQ. It verifies repeatable migrations, all Supervisor processes, non-root execution, live identity role reads, anonymous private-route rejection, durable incident/assignment inbox records, owner-only access, explicit unconfigured mail, null-safe SQL coverage/gaps, dependency outage readiness and recovery. Persistent disposable database volumes ensure restart tests retain real records. .NET Resend transport tests use a fake API key/HTTP handler and verify the configured sender route, text content, idempotency key, correlation ID and rejection handling without sending email. Actual Resend authorization/mailbox delivery remains a staging acceptance check.

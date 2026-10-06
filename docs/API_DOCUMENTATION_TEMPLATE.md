# Service API Documentation Checklist

This is a documentation checklist, not a claim that every endpoint uses one identity provider or token. The current routes and MQTT payload are in [API-SPEC.md](../API-SPEC.md).

For each endpoint document:

- Service, version, base path, method, request and response schemas, status codes, and error behavior.
- The route-specific identity: DeviceService validates dashboard JWTs or its provisioning token; TelemetryService uses a scoped MQTT subscriber identity and the dashboard bridge uses a separate ingestion token.
- Minimum role or service identity. Device reads allow viewer and higher; device registration/update requires engineer/admin.
- Input validation, pagination, idempotency, rate limits, and retention where applicable.
- Sensitive fields and the required TLS and network boundary.

Never put working credentials, user data, or production hostnames in examples. Document JWT issuer, audience, signature algorithm, and required claims. Keep machine-to-machine credentials independent of user sessions. Add authorization allow/deny tests for protected routes.

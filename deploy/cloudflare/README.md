# Cloudflare Split Deployment

This folder contains env templates for deploying background services on Cloudflare:

- `telemetry-service.env` -> `SERVICE_NAME=TelemetryService`
- `analytics-service.env` -> `SERVICE_NAME=AnalyticsService`

Use the same root `Dockerfile` image build, and set the matching `SERVICE_NAME` for each Cloudflare deployment.

Notes:

- `TelemetryService` and `AnalyticsService` are worker-style services. They do not need public HTTP routing.
- Keep `DeviceService`, `IdentityService`, and `NotificationService` on Render with `deploy/render/backend-render.env`.

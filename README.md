# Smart Factory IoT Backend

.NET 8 microservices backend for device management, telemetry ingestion, analytics, and notifications.

## Runtime Stack

- Event bus: RabbitMQ (`AMQP`)
- IoT ingestion: MQTT subscriber (`TelemetryService`) + HTTP telemetry endpoint
- Data: MySQL
- Services:
1. `DeviceService` (REST)
2. `IdentityService` (REST/auth)
3. `NotificationService` (event-driven + REST)
4. `AnalyticsService` (event-driven)
5. `TelemetryService` (functions worker + MQTT)

## Prerequisites

1. Docker Desktop
2. .NET SDK 8+

## Run Backend Locally

From this repo:

```bash
docker compose up --build -d
```

Services/ports:

1. `DeviceService` -> `http://localhost:5001`
2. `IdentityService` -> `http://localhost:5002`
3. `NotificationService` -> `http://localhost:5003`
4. `AnalyticsService` -> `http://localhost:5004`
5. `TelemetryService` -> `http://localhost:5005`
6. MySQL -> `localhost:3306` (`root` / `rootpassword`)
7. RabbitMQ AMQP -> `localhost:5672`
8. RabbitMQ MQTT -> `localhost:1883`
9. RabbitMQ UI -> `http://localhost:15672` (`guest` / `guest`)

## Infrastructure Setup Details

### Local Infrastructure

`docker-compose.yml` provisions:

1. RabbitMQ with AMQP and MQTT listeners
2. MySQL for service persistence
3. All backend microservices

Recommended local checks:

1. RabbitMQ UI reachable at `http://localhost:15672`
2. Device service health endpoint responds
3. Telemetry events flow through broker into analytics/notification services

### Production Infrastructure

For cloud deployment, provision:

1. Container runtime (AKS/ECS/Render/Fly)
2. RabbitMQ (managed or self-hosted)
3. MySQL (managed)
4. Secret manager for service credentials

If deploying this repo with the root `Dockerfile` on Render:

1. Use the repo root Dockerfile path.
2. Set environment variable `SERVICE_NAME` per Render service:
   - `DeviceService`
   - `IdentityService`
   - `NotificationService`
   - `AnalyticsService`
   - `TelemetryService`

The container now publishes all services at build time and runs the one selected by `SERVICE_NAME` at runtime.

Minimum secrets per environment:

- RabbitMQ connection string/user/password
- MySQL connection string/user/password
- Service auth/JWT secrets
- Optional email/SMS provider keys for notifications

## End-to-End Validation Flow

1. Start backend stack.
2. Start frontend stack from `smart-factory-iot`.
3. Register/login from frontend.
4. Create device and update thresholds.
5. Publish telemetry via MQTT and confirm alert/analytics updates.
6. Validate OTA endpoints and notification configs.

## Tests

```bash
DOTNET_ROLL_FORWARD=Major dotnet test src/SmartFactory.Tests/SmartFactory.Tests.csproj -v minimal
DOTNET_ROLL_FORWARD=Major dotnet test src/SmartFactory.sln -v minimal
```

## CI/CD Notes

- GitHub Actions builds/tests on every PR and push.
- Docker publish to ACR runs on `main` pushes.
- Required secrets for image push:
  - `ACR_USERNAME`
  - `ACR_PASSWORD`

If ACR credentials are missing or invalid, Docker publish is skipped/fails safely while test/build still run.

## Companion Frontend Repo

Frontend full-stack app is in:

`/Users/andrewgotora/Software Development/GitHub/smart-factory-iot`

Frontend DB policy there:

- Development: local PostgreSQL
- Production: managed/cloud PostgreSQL

Important: this backend currently uses MySQL and RabbitMQ. It does not directly share the frontend PostgreSQL schema.

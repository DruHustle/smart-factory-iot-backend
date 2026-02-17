# Smart Factory IoT Backend

.NET 8 microservices backend for industrial telemetry, device management, analytics, and notifications.

## Current Runtime Stack

- Event bus: RabbitMQ (`AMQP`)
- IoT ingestion: MQTT subscriber (`TelemetryService`) + HTTP telemetry endpoint
- Data: MySQL
- Services:
1. `DeviceService` (REST)
2. `TelemetryService` (Functions worker + MQTT ingestion)
3. `AnalyticsService` (event-driven)
4. `NotificationService` (event-driven + REST)
5. `IdentityService` (Microsoft Identity Web)

## Frontend Alignment Status

Frontend repo reviewed:  
`/Users/andrewgotora/Software Development/GitHub/smart-factory-iot`

Compatibility/auth fixes applied there:
1. Registration now issues JWT + session cookie (same behavior as login).
2. Frontend auth default API base URL corrected to `http://localhost:3000/api`.
3. Auth storage handling centralized via shared token helpers.
4. Missing tRPC server procedure contracts added so frontend type-check passes.

Note: frontend unit tests still require a running database (`DATABASE_URL`) and fail without it.

## Step-By-Step Dev Run (Backend + Frontend)

### 1. Prerequisites

1. Docker Desktop
2. .NET SDK 8+
3. Node.js 20+
4. `pnpm` 10+

### 2. Start This Backend

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

### 3. Start Frontend Full-Stack Repo

From frontend repo:
`/Users/andrewgotora/Software Development/GitHub/smart-factory-iot`

1. Install deps
```bash
pnpm install
```

2. Create `.env` (or use existing) with:
```bash
NODE_ENV=development
PORT=3000
VITE_API_URL=http://localhost:3000/api
DATABASE_URL=mysql://root:rootpassword@localhost:3306/SmartFactory
JWT_SECRET=dev-secret-key
```

3. Run frontend server + API:
```bash
pnpm dev
```

This starts:
1. UI on `http://localhost:3000`
2. tRPC/API on `http://localhost:3000/api/trpc`
3. Auth endpoints on `http://localhost:3000/api/auth/*`

### 4. Authentication Smoke Test (Frontend)

1. Open `http://localhost:3000`
2. Register a new account
3. Verify redirect into app
4. Refresh browser (session should persist)
5. Logout and verify return to login

### 5. Backend Test Commands (This Repo)

```bash
DOTNET_ROLL_FORWARD=Major dotnet test src/SmartFactory.Tests/SmartFactory.Tests.csproj -v minimal
DOTNET_ROLL_FORWARD=Major dotnet test src/SmartFactory.sln -v minimal
```

## Known Gaps

1. Frontend tests in `smart-factory-iot` depend on DB state and will fail with `Database not available` unless `DATABASE_URL` is reachable and seeded.
2. Frontend currently uses its own Node/tRPC backend API contract; this .NET microservice repo is not yet exposed behind a unified API gateway matching that contract.

# API Specification Overview: Smart Factory IoT Backend

This document provides a high-level overview of the RESTful APIs exposed by the Smart Factory microservices. All services are documented using **OpenAPI (Swagger)** for interactive exploration and **AsyncAPI** for event-driven communication.

## 1. RESTful API Endpoints (OpenAPI)

All synchronous communication is handled via RESTful APIs. Each service exposes its own API, accessible via a Kubernetes Ingress Controller in a production environment.

| Service | Base Path (Ingress) | Primary Resources | Key Operations | Authentication |
| :--- | :--- | :--- | :--- | :--- |
| **DeviceService** | `/api/v1/devices` | `Device`, `DeviceType`, `Firmware` | CRUD for device metadata, firmware updates. | JWT (Bearer) |
| **IdentityService** | `/api/v1/identity` | `User`, `Role`, `Token` | User registration, login, token validation, user management. | OAuth2/OpenID Connect |
| **AnalyticsService** | `/api/v1/analytics` | `OEE`, `AnomalyReport` | Retrieve OEE reports, fetch anomaly history, configure thresholds. | JWT (Bearer) |
| **NotificationService** | `/api/v1/notifications` | `Alert`, `Subscription` | Manage alert subscriptions, trigger manual notifications. | JWT (Bearer) |
| **TelemetryService** | `/api/v1/telemetry` | `TelemetryData` | Retrieve historical telemetry data, device status. | JWT (Bearer) |

### 1.1. API Documentation Access

The interactive Swagger UI for each service is available at:
*   `http://<ingress-host>/<service-name>/swagger`

## 2. Asynchronous API (AsyncAPI)

The backend uses an event-driven architecture for inter-service communication and real-time data streams, specified using **AsyncAPI**.

### 2.1. Event Bus (Azure Service Bus)

The `BuildingBlocks/EventBus` component abstracts the message broker. Events are published by one service and consumed by others.

| Event | Publisher | Consumers | Purpose |
| :--- | :--- | :--- | :--- |
| `NewTelemetryEvent` | `TelemetryService` | `AnalyticsService` | Signals new raw telemetry data for processing. |
| `AnomalyDetectedEvent` | `AnalyticsService` | `NotificationService` | Triggers an alert when an anomaly is detected. |
| `DeviceStatusChangedEvent` | `DeviceService` | `TelemetryService` | Informs other services of a device's lifecycle change (e.g., offline, maintenance). |

### 2.2. Real-time Telemetry (Azure SignalR Service)

The `TelemetryService` broadcasts real-time updates to connected frontend clients via SignalR Hubs.

| Hub | Message Type | Data Payload | Purpose |
| :--- | :--- | :--- | :--- |
| `TelemetryHub` | `ReceiveTelemetryUpdate` | `TelemetryDataDto` | Live stream of sensor readings (temperature, vibration, etc.). |
| `AlertsHub` | `ReceiveAlert` | `AlertDto` | Real-time notification of critical events (e.g., OEE drop, anomaly). |

The full AsyncAPI specification is located at `docs/asyncapi.yaml`.

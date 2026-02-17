# End-to-End Integration Test Strategy for Data Pipeline

This document outlines the strategy for verifying the entire data pipeline, from simulated data ingestion to final notification. It provides both a conceptual testing strategy for automated tests and a **step-by-step guide for manual local testing** using Docker and local tools.

## 1. Data Pipeline Flow Overview

The smart factory data pipeline involves five core microservices and the Event Bus. The flow is triggered by a telemetry message that contains values exceeding predefined thresholds.

| Step | Service | Action | Expected Outcome |
| :--- | :--- | :--- | :--- |
| **1. Ingestion** | `TelemetryService` | Receives telemetry data via REST/IoT Hub. | Saves to PostgreSQL, broadcasts via SignalR, publishes `TelemetryReceivedEvent`. |
| **2. Analysis** | `AnalyticsService` | Consumes `TelemetryReceivedEvent`. | Detects anomaly (Temp > 80), publishes `AnomalyDetectedEvent`. |
| **3. Notification** | `NotificationService` | Consumes `AnomalyDetectedEvent`. | Triggers notification logic (Logs/Email/Logic App). |

## 2. Local Testing Guide (Step-by-Step)

Follow these instructions to verify the application is running correctly in your local development environment.

### Prerequisites
- **Docker & Docker Compose** installed.
- **Postman** or **cURL** for API testing.
- **PostgreSQL Client** (optional, e.g., DBeaver or pgAdmin).

### Step 1: Start the Infrastructure
Launch all services and the database using the optimized Docker Compose configuration.
```bash
# Navigate to the root directory
docker-compose up --build -d
```
*Wait approximately 30-60 seconds for PostgreSQL to initialize and services to start.*

### Step 2: Verify Service Health
Check if all services are healthy using their health check endpoints.
```bash
# Verify Device Service
curl http://localhost:5001/health

# Verify Telemetry Service
curl http://localhost:5005/health
```
*Expected Response: `Healthy` or `200 OK`*

### Step 3: Simulate Telemetry Ingestion
Send a "High Temperature" telemetry data point to the `TelemetryService`.
```bash
curl -X POST http://localhost:5005/api/v1/telemetry \
-H "Content-Type: application/json" \
-d '{
  "DeviceId": "FACTORY-01-CNC",
  "Temperature": 95.5,
  "Humidity": 42.0,
  "Vibration": 1.5,
  "Timestamp": "2026-01-29T12:00:00Z"
}'
```

### Step 4: Verify Data Persistence
Check if the data was successfully saved to the local PostgreSQL database.
```bash
# Connect to PostgreSQL (Password: postgrespassword)
docker exec -it sf-postgres psql -U postgres -d SmartFactory -c "SELECT * FROM \"TelemetryRecords\" WHERE \"DeviceId\"='FACTORY-01-CNC';"
```

### Step 5: Verify the Event Pipeline (Logs)
Since the temperature (95.5) is > 80, the `AnalyticsService` should detect an anomaly. Check the logs of the services to see the event flow.
```bash
# Check Analytics Service logs for anomaly detection
docker logs sf-analytics-service | grep "AnomalyDetected"

# Check Notification Service logs for alert triggering
docker logs sf-notification-service | grep "Sending notification"
```

## 3. Automated Integration Test Strategy (C#)

For automated CI/CD verification, we use mocked abstractions of external dependencies while running the real domain logic.

### Key Mocking Requirements
| Dependency | Interface | Purpose |
| :--- | :--- | :--- |
| **Event Bus** | `IEventBus` | Capture and verify published integration events. |
| **Database** | `DbContext` | Use `InMemory` or `SQLite` for fast, isolated tests. |

### Conceptual Test Implementation
```csharp
[Fact]
public async Task HighTemperature_ShouldTriggerAnomalyEvent()
{
    // 1. ARRANGE
    var mockEventBus = new Mock<IEventBus>();
    var telemetryService = new TelemetryService(mockEventBus.Object, ...);
    var analyticsHandler = new TelemetryReceivedEventHandler(new AnalyticsEngine(), mockEventBus.Object);

    // 2. ACT
    // Simulate ingestion
    await telemetryService.ProcessTelemetryAsync(new TelemetryDto { Temperature = 95 });
    
    // Simulate Event Bus delivering the event to Analytics
    var capturedEvent = new TelemetryReceivedEvent { Temperature = 95 };
    await analyticsHandler.Handle(capturedEvent);

    // 3. ASSERT
    mockEventBus.Verify(eb => eb.PublishAsync(It.IsAny<AnomalyDetectedEvent>()), Times.Once());
}
```

## 4. Troubleshooting Local Setup
- **PostgreSQL Connection Refused**: Ensure the `sf-postgres` container is healthy (`docker ps`).
- **Port Conflicts**: Ensure ports 5001-5005 and 3306 are not being used by other applications.
- **Service Crashing**: Check logs using `docker logs <container_name>` to identify missing environment variables or configuration errors.

***
*Document Author: Manus AI (on behalf of DruHustle)*

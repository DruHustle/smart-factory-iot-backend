# End-to-End Integration Test Strategy for Data Pipeline

This document outlines the strategy for creating a single, comprehensive integration test that verifies the entire data pipeline, from simulated data ingestion to final notification, using a single data point. This approach ensures that all service boundaries and integration points are functioning correctly.

## 1. Data Pipeline Flow Overview

The smart factory data pipeline involves four main services and the Event Bus building block. The flow is triggered by a single telemetry message that is intentionally designed to cause an anomaly.

| Step | Service | Component | Action | Expected Outcome |
| :--- | :--- | :--- | :--- | :--- |
| **1. Ingestion** | TelemetryService | `TelemetryFunction.Run` | Receives IoT Hub message (JSON). | Saves to DB, broadcasts via SignalR, publishes `TelemetryReceivedIntegrationEvent`. |
| **2. Analysis** | AnalyticsService | `TelemetryReceivedIntegrationEventHandler` | Consumes `TelemetryReceivedIntegrationEvent`. | Detects anomaly, publishes `AnomalyDetectedIntegrationEvent`. |
| **3. Notification** | NotificationService | `AnomalyDetectedIntegrationEventHandler` | Consumes `AnomalyDetectedIntegrationEvent`. | Triggers Logic App (`TriggerLogicAppAsync`), sends email (`SendEmailAsync`). |

## 2. The Single Data Point for Testing

To ensure the entire pipeline is exercised, the test data point must contain values that trigger the anomaly detection logic in the `AnalyticsEngine`.

**Anomaly Detection Logic (from `AnalyticsEngine.cs`):**
```csharp
IsAnomaly = vibration > 5.0 || temperature > 80.0
```

**Test Data Point (JSON Payload):**

```json
{
  "DeviceId": "TEST-E2E-001",
  "Temperature": 95.0,
  "Humidity": 45.0,
  "Vibration": 2.0,
  "Timestamp": "2026-01-23T10:00:00Z"
}
```

This data point will trigger a **High Temperature** anomaly, which is sufficient to test the full notification path.

## 3. Integration Test Implementation Strategy

A robust integration test for this scenario requires mocking external dependencies while using the real internal logic of the services.

### Key Mocking Requirements

| Dependency | Interface to Mock | Purpose |
| :--- | :--- | :--- |
| **Event Bus** | `IEventBus` | Capture the published events (`TelemetryReceivedIntegrationEvent` and `AnomalyDetectedIntegrationEvent`) for manual processing in the test. |
| **Notification** | `INotificationService` | Verify that the final notification methods (`SendEmailAsync`, `TriggerLogicAppAsync`) are called with the correct payload. |
| **Database** | `TelemetryDbContext` | Use an in-memory database (e.g., `Microsoft.EntityFrameworkCore.InMemory`) to verify data persistence without connecting to a real SQL server. |

### Conceptual Test Structure (C#)

The test should be structured to simulate the asynchronous, decoupled nature of the microservices by manually invoking the event handlers in sequence, using the mocked `IEventBus` to pass the events.

```csharp
[Fact]
public async Task SingleDataPoint_ShouldTriggerFullPipeline_ToNotification()
{
    // ARRANGE: Setup Mocks and Services
    var mockEventBus = new Mock<IEventBus>();
    var mockNotificationService = new Mock<INotificationService>();
    var dbContext = CreateInMemoryDbContext(); // Helper to create TelemetryDbContext in-memory
    var loggerFactory = new LoggerFactory();

    // 1. TelemetryService Setup
    var telemetryFunction = new TelemetryFunction(loggerFactory, dbContext, mockEventBus.Object);
    var testJsonPayload = "{\"DeviceId\":\"TEST-E2E-001\", \"Temperature\":95.0, \"Humidity\":45.0, \"Vibration\":2.0, \"Timestamp\":\"2026-01-23T10:00:00Z\"}";

    // 2. AnalyticsService Setup
    var analyticsEngine = new AnalyticsEngine();
    var analyticsHandler = new TelemetryReceivedIntegrationEventHandler(
        analyticsEngine, 
        loggerFactory.CreateLogger<TelemetryReceivedIntegrationEventHandler>(), 
        mockEventBus.Object
    );

    // 3. NotificationService Setup
    var notificationHandler = new AnomalyDetectedIntegrationEventHandler(
        mockNotificationService.Object, 
        loggerFactory.CreateLogger<AnomalyDetectedIntegrationEventHandler>()
    );

    // --- ACT: Simulate the Pipeline ---

    // Step 1: Simulate Ingestion (TelemetryService)
    var signalRAction = telemetryFunction.Run(testJsonPayload, /* mock FunctionContext */).Result;

    // ASSERT 1: Verify TelemetryService actions
    Assert.NotNull(signalRAction); // SignalR message was returned
    Assert.Equal(1, await dbContext.TelemetryRecords.CountAsync()); // Data saved to DB
    
    // Capture the published TelemetryReceivedIntegrationEvent
    TelemetryReceivedIntegrationEvent? telemetryEvent = null;
    mockEventBus.Verify(
        eb => eb.PublishAsync(It.Is<IntegrationEvent>(e => e is TelemetryReceivedIntegrationEvent)),
        Times.Once(),
        "TelemetryReceivedIntegrationEvent was not published."
    );
    // Note: In a real test, you would use a callback on the mock to capture the event object.

    // Step 2: Simulate Event Bus Delivery to AnalyticsService
    // Manually create the event object based on the input data for the test
    telemetryEvent = new TelemetryReceivedIntegrationEvent("TEST-E2E-001", 95.0, 45.0, 2.0, DateTime.Parse("2026-01-23T10:00:00Z"));
    await analyticsHandler.Handle(telemetryEvent);

    // ASSERT 2: Verify AnalyticsService actions
    // Capture the published AnomalyDetectedIntegrationEvent
    AnomalyDetectedIntegrationEvent? anomalyEvent = null;
    mockEventBus.Verify(
        eb => eb.PublishAsync(It.Is<IntegrationEvent>(e => e is AnomalyDetectedIntegrationEvent)),
        Times.Once(),
        "AnomalyDetectedIntegrationEvent was not published."
    );
    // Note: Again, use a callback to capture the event object.

    // Step 3: Simulate Event Bus Delivery to NotificationService
    // Manually create the anomaly event object for the test
    anomalyEvent = new AnomalyDetectedIntegrationEvent("TEST-E2E-001", "HighTemperature", 95.0, DateTime.Parse("2026-01-23T10:00:00Z"));
    await notificationHandler.Handle(anomalyEvent);

    // ASSERT 3: Verify NotificationService actions
    mockNotificationService.Verify(
        ns => ns.TriggerLogicAppAsync(It.Is<AlertPayload>(p => p.DeviceId == "TEST-E2E-001" && p.Severity == "Critical")),
        Times.Once(),
        "Logic App was not triggered."
    );
    mockNotificationService.Verify(
        ns => ns.SendEmailAsync(It.Is<EmailRequest>(r => r.Subject.Contains("Anomaly Detected"))),
        Times.Once(),
        "Email notification was not sent."
    );
}
```

## 4. Conclusion

By following this strategy, you can create a single, powerful integration test that verifies the complex, multi-service data flow. This test isolates the core business logic of the services from the complexities of the cloud infrastructure (IoT Hub, Service Bus, SignalR, Logic Apps), providing fast, reliable, and repeatable verification of the entire pipeline.

***
*Document Author: Andrew Gotora*

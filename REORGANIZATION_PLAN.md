# Smart Factory IoT Backend Reorganization Plan

This document outlines the architectural changes made to the Smart Factory IoT Backend to align with microservices standards and SOLID principles.

## 1. Architectural Overview

The project has been reorganized from a flat structure into a multi-layered microservices architecture. Each service now follows a consistent folder structure:

- **API**: Controllers and Program.cs (entry point).
- **Application**: DTOs, Interfaces, and Application Services.
- **Domain**: Entities and Domain Models.
- **Infrastructure**: Data Contexts (EF Core) and Repository implementations.

## 2. Services Reorganized

### DeviceService
- **Domain**: Added `Device` entity.
- **Infrastructure**: Implemented `DeviceDbContext` and `DeviceRepository`.
- **Application**: Added `DeviceDto`.
- **API**: Refactored `DevicesController` to use the repository pattern.

### IdentityService
- **Application**: Added `IIdentityService` and its implementation to encapsulate Microsoft Identity Web logic.
- **API**: Refactored `AuthController` to use the service.

### AnalyticsService
- **Domain**: Added `IndustrialMetrics` model.
- **Application**: Refactored `AnalyticsEngine` and moved `AnomalyDetectedIntegrationEvent` to the Application layer.

### TelemetryService
- **Domain**: Added `TelemetryRecord` entity.
- **Application**: Added `TelemetryData` DTO.
- **Infrastructure**: Added `TelemetryDbContext`.
- **Function**: Refactored `TelemetryFunction` to use the new models and context.

### NotificationService
- **Application**: Added `INotificationService`, `EmailRequest`, and `AlertPayload`.
- **Infrastructure**: Implemented `NotificationService` using Microsoft Graph.
- **API**: Refactored `NotificationsController`.

## 3. Building Blocks

### EventBus
- **Abstractions**: Added `IEventBus` and `IIntegrationEventHandler`.
- **Models**: Added base `IntegrationEvent`.
- **Implementations**: Refactored `AzureServiceBus` to implement the new interface.

## 4. SOLID Principles Applied

- **Single Responsibility Principle (SRP)**: Each class now has a single reason to change. Controllers only handle HTTP requests, services handle business logic, and repositories handle data access.
- **Interface Segregation Principle (ISP)**: Services and repositories are accessed through lean interfaces.
- **Dependency Inversion Principle (DIP)**: High-level modules (Controllers) depend on abstractions (Interfaces) rather than low-level implementations.

## 5. Testing

- Updated `TelemetryParsingTests` to reflect new namespaces.
- Added `AnalyticsServiceTests` to verify core business logic.
- All tests passed successfully.

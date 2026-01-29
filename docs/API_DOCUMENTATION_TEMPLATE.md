# API Documentation Template (OpenAPI/Swagger)

This template outlines the structure for generating comprehensive OpenAPI/Swagger documentation for each microservice.

## 1. Overview

*   **Service Name**: [e.g., DeviceService]
*   **Base URL**: `/api/v1/[service-name]`
*   **Version**: v1.0
*   **Description**: A brief, one-paragraph description of the service's primary function.

## 2. Authentication

All endpoints require a valid **JWT Bearer Token** obtained from the `IdentityService`.

*   **Security Scheme**: OAuth2 (Implicit Flow or Client Credentials Flow)
*   **Scopes**: [List required scopes, e.g., `device.read`, `device.write`]

## 3. Endpoints

### 3.1. [Resource Name] Management

**Resource**: `/api/v1/[service-name]/[resource]` (e.g., `/api/v1/devices/device`)

| HTTP Method | Path | Description | Request Body | Responses |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/` | Retrieves a paginated list of all [Resource Name]. | None | `200 OK` (List of [Resource Name] DTOs) |
| `GET` | `/{id}` | Retrieves a single [Resource Name] by ID. | None | `200 OK` ([Resource Name] DTO), `404 Not Found` |
| `POST` | `/` | Creates a new [Resource Name]. | `Create[Resource Name]Command` | `201 Created` ([Resource Name] DTO) |
| `PUT` | `/{id}` | Updates an existing [Resource Name]. | `Update[Resource Name]Command` | `204 No Content`, `404 Not Found` |
| `DELETE` | `/{id}` | Deletes a [Resource Name] by ID. | None | `204 No Content`, `404 Not Found` |

## 4. Data Transfer Objects (DTOs)

### 4.1. [Resource Name] DTO

| Field | Type | Description | Example |
| :--- | :--- | :--- | :--- |
| `id` | `UUID` | Unique identifier for the resource. | `a1b2c3d4-e5f6-7890-1234-567890abcdef` |
| `name` | `string` | Human-readable name. | `CNC Machine 001` |
| `status` | `string` | Current operational status. | `Active` |
| `lastUpdated` | `datetime` | Timestamp of the last update. | `2026-01-29T10:00:00Z` |

## 5. Commands and Queries

### 5.1. Create[Resource Name]Command

| Field | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `name` | `string` | Yes | The name of the new resource. |
| `type` | `string` | Yes | The type of the resource (e.g., `ESP32`, `Gateway`). |

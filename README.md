# Smart Factory IoT Backend

A robust, scalable .NET-based microservices backend designed for real-time industrial monitoring and control. This repository provides the core infrastructure and services to connect edge devices (ESP-32 Wrover) via an IoT gateway (Raspberry Pi 5) to the Azure cloud ecosystem.

## 🚀 Key Features

*   **Microservices Architecture**: Built with .NET 8, following Domain-Driven Design (DDD) and S.O.L.I.D. principles for high maintainability and scalability.
*   **IoT Integration**: Seamless connectivity with **Azure IoT Hub** for telemetry ingestion, device twin synchronization, and cloud-to-device (C2D) commands.
*   **Real-time Analytics**: Dedicated analytics engine for calculating **Overall Equipment Effectiveness (OEE)** and detecting sensor anomalies (temperature, humidity, vibration).
*   **Edge-to-Cloud Connectivity**: Optimized for **Raspberry Pi 5** gateways and **ESP-32 Wrover** edge nodes using MQTT/AMQP protocols.
*   **Infrastructure as Code (IaC)**: Fully automated resource provisioning using **Terraform** and **Azure Bicep**.
*   **Cloud-Native Deployment**: Containerized with **Docker** and orchestrated via **Azure Kubernetes Service (AKS)**.
*   **Real-time Dashboards**: Integrated with **Azure SignalR Service** to push live telemetry updates to the frontend.
*   **Enterprise Security**: Secured with **Microsoft Entra ID (Azure AD)**, RBAC, and **Azure Key Vault** for secrets management.

## 🏗️ Architectural Overview

The project is organized into a multi-layered microservices architecture. Each service follows a consistent folder structure to ensure separation of concerns and maintainability:

-   **API**: Controllers and entry points.
-   **Application**: DTOs, Interfaces, and Application Services.
-   **Domain**: Entities and Domain Models.
-   **Infrastructure**: Data Contexts (EF Core) and Repository implementations.

### Core Services

-   **DeviceService**: Manages industrial device metadata, status, and lifecycle.
-   **IdentityService**: Encapsulates Microsoft Identity Web logic for secure authentication.
-   **AnalyticsService**: Processes sensor data to calculate OEE and detect anomalies.
-   **TelemetryService**: Ingests IoT Hub messages, persists data, and broadcasts updates via SignalR.
-   **NotificationService**: Triggers alerts and sends email notifications via Microsoft Graph and Logic Apps.

### Building Blocks

-   **EventBus**: Provides an abstraction for asynchronous communication between services using Azure Service Bus.

## 🛠️ Technology Stack

| Category | Technologies |
| :--- | :--- |
| **Frameworks** | .NET 8, ASP.NET Core Web API, Azure Functions |
| **Cloud (Azure)** | IoT Hub, SignalR, Key Vault, Logic Apps, API Management |
| **DevOps** | Docker, Kubernetes (AKS), GitHub Actions, Terraform |
| **Data** | Aiven MySQL, Azure Redis Cache, EF Core |
| **Communication** | REST, SignalR, MQTT, AMQP, AsyncAPI |

## 📂 Repository Structure

*   `/src`: Contains the .NET microservices and shared building blocks.
*   `/deploy`: Infrastructure as Code (Terraform) and Kubernetes manifests.
*   `/docs`: Comprehensive deployment guides and API specifications (OpenAPI/AsyncAPI).
*   `/.github`: CI/CD pipeline definitions for automated workflows.

## 📖 Getting Started

For detailed setup and deployment instructions, please refer to the [Deployment Guide](./docs/Deployment_Guide.md).

---
*This project is designed for industrial-grade reliability and performance, utilizing Azure's free tier services where possible for cost-effective development.*

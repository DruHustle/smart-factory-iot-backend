# Smart Factory IoT Backend

A robust, scalable .NET-based microservices backend designed for real-time industrial monitoring and control. This repository provides the core infrastructure and services to connect edge devices (ESP-32 Wrover) via an IoT gateway (Raspberry Pi 5) to the Azure cloud ecosystem.

## 🚀 Key Features

*   **Microservices Architecture**: Built with .NET 8, following **Domain-Driven Design (DDD)** and **S.O.L.I.D.** principles for high maintainability and scalability.
*   **IoT Integration**: Seamless connectivity with **Azure IoT Hub** for telemetry ingestion, device twin synchronization, and cloud-to-device (C2D) commands.
*   **Real-time Analytics**: Dedicated analytics engine for calculating **Overall Equipment Effectiveness (OEE)** and detecting sensor anomalies (temperature, humidity, vibration).
*   **Cloud-Native Deployment**: Containerized with **Docker** and orchestrated via **Azure Kubernetes Service (AKS)**.
*   **Infrastructure as Code (IaC)**: Fully automated resource provisioning using **Terraform**.
*   **Comprehensive CI/CD**: Automated build, test, and push to Azure Container Registry (ACR) via **GitHub Actions**.
*   **Real-time Dashboards**: Integrated with **Azure SignalR Service** to push live telemetry updates to the frontend.
*   **Enterprise Security**: Secured with **Microsoft Entra ID (Azure AD)**, RBAC, and **Azure Key Vault** for secrets management.

## 🏗️ Architectural Overview

The project is organized into five core microservices, each representing a distinct bounded context. For a detailed explanation of the architecture and principles, please see [ARCHITECTURE.md](./ARCHITECTURE.md).

| Service | Primary Function | Data Store |
| :--- | :--- | :--- |
| **DeviceService** | Manages industrial device metadata, status, and lifecycle. | MySQL |
| **IdentityService** | Encapsulates Microsoft Identity Web logic for secure authentication. | MySQL |
| **AnalyticsService** | Processes sensor data to calculate OEE and detect anomalies. | None (Event-driven) |
| **TelemetryService** | Ingests IoT Hub messages, persists data, and broadcasts updates via SignalR. | MySQL |
| **NotificationService** | Triggers alerts and sends email notifications via Microsoft Graph and Logic Apps. | None (Event-driven) |

## 🛠️ Technology Stack

| Category | Technologies |
| :--- | :--- |
| **Frameworks** | .NET 8, ASP.NET Core Web API, Azure Functions |
| **Cloud (Azure)** | IoT Hub, SignalR, Key Vault, Logic Apps, API Management |
| **DevOps** | Docker, Kubernetes (AKS), GitHub Actions, Terraform |
| **Data** | MySQL (Local/Aiven), Azure Redis Cache, EF Core |
| **Communication** | REST, SignalR, MQTT, AMQP, AsyncAPI |

## 📂 Repository Structure

| Directory | Description |
| :--- | :--- |
| `/src` | Contains the .NET microservices, shared building blocks, and test projects. |
| `/deploy` | **Deployment assets:** Kubernetes manifests (`/k8s`), Terraform IaC (`/terraform`), and local development scripts (`/local-dev`). |
| `/docs` | **Documentation:** Deployment guide, API specifications, and architecture details. |
| `/.github` | **CI/CD:** GitHub Actions workflow definitions. |
| `docker-compose.yml` | Configuration for running all services locally with a MySQL database. |
| `ARCHITECTURE.md` | Detailed explanation of the architectural design and principles. |
| `API-SPEC.md` | High-level overview of all RESTful and Asynchronous APIs. |

## 📖 Getting Started

### 1. Prerequisites

Ensure you have **Docker**, **Docker Compose**, and the **.NET 8 SDK** installed.

### 2. Local Development Setup

The easiest way to run the entire backend locally is using Docker Compose.

1.  **Build and Run Services**: This command will build the Docker images for all microservices and start the MySQL database.
    ```bash
    docker-compose up --build
    ```
2.  **Access Services**: The services will be available on `http://localhost:5001` through `http://localhost:5005`.
3.  **Database Details**: The local MySQL instance is available at `localhost:3306` with `User: root` and `Password: rootpassword`.

For detailed setup, deployment to Kubernetes, and cloud configuration, please refer to the [Deployment Guide](./docs/Deployment_Guide.md).

### 3. Frontend Integration

For instructions on how to integrate a React or other SPA frontend, including authentication with the `IdentityService` and real-time data via `SignalR`, see **Section 6** of the [Deployment Guide](./docs/Deployment_Guide.md).

# Smart Factory IoT Backend Deployment Guide

## 1. Introduction and Architecture Overview

This document provides a comprehensive, step-by-step guide for deploying the **Smart Factory IoT Backend** built on a **.NET Microservices Architecture**. The system is designed for real-time industrial monitoring, integrating edge devices with Azure cloud services.

The architecture adheres to **Domain-Driven Design (DDD)** and **S.O.L.I.D. Principles**, utilizing containerization for portability and scalability.

| Component Layer | Key Technologies | Purpose |
| :--- | :--- | :--- |
| **Edge Layer** | Raspberry Pi 5, ESP32 Rover, MQTT/AMQP | Data acquisition and gateway functionality. |
| **IoT Layer** | Azure IoT Hub | Central hub for device connectivity, telemetry ingestion, and command & control (C2D). |
| **Backend Services** | .NET 8 Microservices (Identity, Device, Telemetry, Analytics, Notification) | Business logic, data processing, and API endpoints. |
| **Data Layer** | Aiven MySQL, Azure Redis Cache | Persistent storage and high-performance caching. |
| **Messaging** | Azure Service Bus | Asynchronous event-driven communication (Event Bus). |
| **Infrastructure** | Terraform, Kubernetes (AKS), Docker | Infrastructure as Code (IaC) and container orchestration. |
| **CI/CD** | GitHub Actions, Azure DevOps | Automated build, test, and deployment pipelines. |

## 2. Prerequisites

Before starting the deployment, ensure you have the following tools and accounts:

1.  **Azure Account**: Subscription with permissions to create resources.
2.  **Azure CLI**: Installed and configured locally.
3.  **Terraform CLI**: Installed locally (version 1.0+).
4.  **Docker**: Installed locally for building and testing containers.
5.  **Kubernetes CLI (kubectl)**: Installed for managing the AKS cluster.
6.  **.NET SDK**: Version 8.0 installed.

## 3. Local Development Setup (Docker Compose)

For local development and testing, you can use the provided \`docker-compose.yml\` file to spin up all microservices and a local MySQL database instance.

### Step 3.1: Start Services

Ensure Docker is running on your machine. The \`docker-compose.yml\` file will build the service images using the respective \`Dockerfile\` in each service directory and start a MySQL container.

\`\`\`bash
cd smart-factory-iot-backend
docker-compose up --build
\`\`\`

The services will be available on the following ports:
*   **Device Service**: \`http://localhost:5001\`
*   **Identity Service**: \`http://localhost:5002\`
*   **Notification Service**: \`http://localhost:5003\`
*   **Analytics Service**: \`http://localhost:5004\`
*   **Telemetry Service**: \`http://localhost:5005\`

The MySQL database is accessible on \`localhost:3306\` with the following credentials:
*   **Database Name**: \`SmartFactory\`
*   **User**: \`root\`
*   **Password**: \`rootpassword\`

### Step 3.2: Database Migrations

You will need to run database migrations for the services that use persistent storage (DeviceService, IdentityService, TelemetryService). This is typically done by running the application locally outside of Docker or by executing a migration command inside the container.

\`\`\`bash
# Example for DeviceService (assuming .NET CLI is installed locally)
dotnet ef database update --project src/Services/DeviceService/DeviceService.csproj
\`\`\`

## 4. Cloud Infrastructure Setup (Terraform)

The core Azure resources are provisioned using the provided Terraform configuration (`deploy/terraform/main.tf`).

### Step 3.1: Initialize and Plan

Navigate to the Terraform directory and initialize the workspace.

```bash
cd smart-factory-backend/deploy/terraform
terraform init
```

Review the plan to ensure the correct resources will be created. **Note**: You must replace `YOUR_TENANT_ID` in `main.tf` with your Azure Active Directory Tenant ID.

```bash
terraform plan -var="tenant_id=<YOUR_TENANT_ID>"
```

### Step 3.2: Apply Configuration

Apply the configuration to create the resources.

```bash
terraform apply -var="tenant_id=<YOUR_TENANT_ID>"
```

This will provision the following **free-tier** Azure services:
*   **Resource Group**: `rg-smart-factory`
*   **Azure IoT Hub**: `iothub-smart-factory` (F1 Free Tier)
*   **Azure Key Vault**: `kv-smart-factory`
*   **Azure SignalR Service**: `signalr-smart-factory` (Free_F1 Tier)
*   **Azure Logic App Workflow**: `logic-smart-factory-notifications` (Requires manual design for email/Teams integration)
*   **Azure Service Bus**: `sb-smart-factory` (Basic Tier for Event Bus)

## 5. Backend Deployment (CI/CD)

The microservices are containerized and deployed to a Kubernetes cluster (e.g., Azure Kubernetes Service - AKS).

### Step 5.1: Build and ContainerizeDockerfiles have been created for all microservices (DeviceService, IdentityService, NotificationService, AnalyticsService, and TelemetryService) and are located in their respective service directories. These Dockerfiles are multi-stage builds optimized for productioThe manual build and push steps are now automated via the GitHub Actions workflow. You only need to ensure your Azure Container Registry (ACR) credentials are set up as GitHub Secrets (\`ACR_USERNAME\` and \`ACR_PASSWORD### Step 5.2: Deploy to KuberneteKubernetes deployment and service manifests have been created for all microservices (DeviceService, IdentityService, NotificationService, AnalyticsService, and TelemetryService) and are located in the \`deploy/k8s\` directory. A manifest for a local MySQL instance (\`mysql.yaml\`) is also included for development cluster deployments.
### Step 5.3: CI/CD PipelineThe GitHub Actions workflow (\`.github/workflows/main.yml\`) has been updated to automatically build, test, and containerize all five microservices upon every push to the \`main\` branch. It uses a matrix strategy to efficiently build and push all service images to your configured Azure Container Registry (ACR).

## 6. Service-Specific Configuration

### 5.1 Identity Service (Microsoft Entra ID)

The Identity Service uses `Microsoft.Identity.Web` for authentication. You must configure an Application Registration in your Microsoft Entra ID tenant and store the following in **Azure Key Vault**:
*   `AzureAd:Instance`
*   `AzureAd:Domain`
*   `AzureAd:TenantId`
*   `AzureAd:ClientId`
*   `AzureAd:ClientSecret`

### 5.2 Notification Service (Microsoft Graph API & Logic Apps)

The Notification Service includes logic to send emails via the **Microsoft Graph API** and trigger an **Azure Logic App**.
*   **Graph API**: Requires the Identity Service to acquire a token with the necessary `Mail.Send` scope.
*   **Logic App**: The `logic-smart-factory-notifications` workflow must be manually designed in the Azure Portal to handle the alert payload and perform actions like sending a Teams message or an email via the Microsoft Graph connector.

## 7. Edge Device Configuration

This section details the configuration for the supported edge devices: **Raspberry Pi 5** and **ESP32 Rover**.

### 6.1 Raspberry Pi 5 (Industrial Gateway)

The Raspberry Pi 5 acts as a high-performance gateway, aggregating data from multiple sensors and providing local edge processing.

1.  **OS Setup**: Use Raspberry Pi OS (64-bit) Lite for optimal performance.
2.  **Azure IoT Edge Runtime**:
    *   Install the IoT Edge runtime to enable containerized module deployment.
    *   Register the device in Azure IoT Hub and apply the connection string.
3.  **Connectivity**:
    *   Use the onboard Ethernet or Wi-Fi 5 for cloud connectivity.
    *   Configure local MQTT broker (e.g., Mosquitto) to receive data from ESP32 nodes.

### 6.2 ESP32 Rover (Sensor Node)

The ESP32 Rover is used for direct sensor interfacing (Temperature, Humidity, Vibration) and low-power telemetry transmission.

1.  **Firmware Development**: Use Arduino IDE or PlatformIO with the `Azure SDK for C` or `PubSubClient` for MQTT.
2.  **Provisioning**:
    *   Use **X.509 Certificates** or **SAS Keys** for secure authentication with Azure IoT Hub.
    *   Configure the device to connect to the Raspberry Pi 5 gateway or directly to IoT Hub via Wi-Fi.
3.  **Power Management**: Utilize deep-sleep modes between telemetry transmissions to optimize battery life in industrial environments.

## 8. API Documentation

The backend services expose RESTful APIs documented using **Swagger/OpenAPI**.

### 7.1 Accessing Swagger UI

Once deployed, you can access the interactive API documentation at the following endpoints:
*   **Device Service**: `http://<k8s-ingress-ip>/device-service/swagger`
*   **Identity Service**: `http://<k8s-ingress-ip>/identity-service/swagger`
*   **Notification Service**: `http://<k8s-ingress-ip>/notification-service/swagger`

### 7.2 AsyncAPI for Telemetry

For real-time telemetry streams and event-driven communication, refer to the `docs/asyncapi.yaml` specification. This defines the message formats for:
*   **IoT Hub Telemetry Ingestion**
*   **Service Bus Integration Events**
*   **SignalR Real-time Updates**

## 9. Next Steps and Frontend Integration

1.  **Database**: Provision and configure your Aiven MySQL instance and update the connection string in the Kubernetes secrets (`db-secrets`).
2.  **Authentication**: Complete the Microsoft Entra ID application registration and ensure all services are configured to use the Identity Service for JWT validation.
3.  **Frontend Integration**: The frontend application (e.g., a React SPA) needs to be configured to interact with the deployed backend services.

    *   **Authentication**: Configure the frontend to use the **Identity Service** for user login. This typically involves redirecting the user to the Identity Service's login endpoint and handling the returned JWT token.
    *   **API Communication**: All RESTful API calls should be directed to the API Gateway (e.g., Azure APIM or a Kubernetes Ingress) which routes traffic to the respective microservices.
    *   **Real-time Telemetry**: Connect the frontend to the **Azure SignalR Service** endpoint. The **Telemetry Service** and **Analytics Service** publish real-time updates (e.g., new telemetry, anomaly alerts) via the Event Bus, which are then broadcasted through SignalR. The frontend should subscribe to the relevant SignalR hubs to display live data.

    \`\`\`javascript
    // Example: Connecting to the SignalR Hub in a React component
    import * as signalR from "@microsoft/signalr";

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("https://<signalr-service-url>/telemetryhub")
        .withAutomaticReconnect()
        .build();

    connection.start().then(() => {
        console.log("Connected to SignalR Telemetry Hub.");
        connection.on("ReceiveTelemetryUpdate", (data) => {
            // Update UI with new telemetry data
            console.log("New telemetry:", data);
        });
    }).catch(err => console.error(err.toString()));
    \`\`\`

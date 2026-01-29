# Smart Factory IoT Backend Deployment Guide

## 1. Introduction and Architecture Overview

This document provides a comprehensive, step-by-step guide for deploying the **Smart Factory IoT Backend** built on a **.NET Microservices Architecture**. The system is designed for real-time industrial monitoring, integrating edge devices with Azure cloud services.

The architecture adheres to **Domain-Driven Design (DDD)** and **S.O.L.I.D. Principles**, utilizing containerization for portability and scalability. For a detailed breakdown of the architecture, refer to the [ARCHITECTURE.md](./ARCHITECTURE.md) file.

| Component Layer | Key Technologies | Purpose |
| :--- | :--- | :--- |
| **Edge Layer** | Raspberry Pi 5, ESP32 Rover, MQTT/AMQP | Data acquisition and gateway functionality. |
| **IoT Layer** | Azure IoT Hub | Central hub for device connectivity, telemetry ingestion, and command & control (C2D). |
| **Backend Services** | .NET 8 Microservices (Identity, Device, Telemetry, Analytics, Notification) | Business logic, data processing, and API endpoints. |
| **Data Layer** | Aiven MySQL, Azure Redis Cache | Persistent storage and high-performance caching. |
| **Messaging** | Azure Service Bus | Asynchronous event-driven communication (Event Bus). |
| **Infrastructure** | Terraform, Kubernetes (AKS), Docker | Infrastructure as Code (IaC) and container orchestration. |
| **CI/CD** | GitHub Actions | Automated build, test, and deployment pipelines. |

## 2. Prerequisites

Before starting the deployment, ensure you have the following tools and accounts:

1.  **Azure Account**: Subscription with permissions to create resources.
2.  **Azure CLI**: Installed and configured locally.
3.  **Terraform CLI**: Installed locally (version 1.0+).
4.  **Docker**: Installed locally for building and testing containers.
5.  **Kubernetes CLI (kubectl)**: Installed for managing the AKS cluster.
6.  **.NET SDK**: Version 8.0 installed.

## 3. Local Development Setup (Docker Compose)

For local development and testing, you can use the provided `docker-compose.yml` file to spin up all microservices and a local MySQL database instance.

### Step 3.1: Start Services

Ensure Docker is running on your machine. The `docker-compose.yml` file will build the service images using the respective `Dockerfile` in each service directory and start a MySQL container.

```bash
# 1. Ensure the local-dev directory exists and contains the init.sql file
mkdir -p deploy/local-dev
# 2. Start all services. The --build flag ensures the latest Dockerfiles are used.
docker-compose up --build
```

The services will be available on the following ports:
*   **Device Service**: `http://localhost:5001`
*   **Identity Service**: `http://localhost:5002`
*   **Notification Service**: `http://localhost:5003`
*   **Analytics Service**: `http://localhost:5004`
*   **Telemetry Service**: `http://localhost:5005`

The MySQL database is accessible on `localhost:3306` with the following credentials:
*   **Database Name**: `SmartFactory`
*   **User**: `root`
*   **Password**: `rootpassword`

### Step 3.2: Database Migrations

The services use Entity Framework Core (EF Core) for data access. You must run database migrations for the services that use persistent storage (`DeviceService`, `IdentityService`, `TelemetryService`).

```bash
# Example for DeviceService (assuming .NET CLI is installed locally)
# Run this command from the root of the repository
dotnet ef database update --project src/Services/DeviceService/DeviceService.csproj
```

## 4. Cloud Infrastructure Setup (Terraform)

The core Azure resources are provisioned using the provided Terraform configuration (`deploy/terraform/main.tf`).

### Step 4.1: Initialize and Plan

Navigate to the Terraform directory and initialize the workspace.

```bash
cd smart-factory-backend/deploy/terraform
terraform init
```

Review the plan to ensure the correct resources will be created. **Note**: You must replace `YOUR_TENANT_ID` in `main.tf` with your Azure Active Directory Tenant ID.

```bash
terraform plan -var="tenant_id=<YOUR_TENANT_ID>"
```

### Step 4.2: Apply Configuration

Apply the configuration to create the resources.

```bash
terraform apply -var="tenant_id=<YOUR_TENANT_ID>"
```

This will provision the following **free-tier** Azure services:
*   **Resource Group**: `rg-smart-factory`
*   **Azure IoT Hub**: `iothub-smart-factory` (F1 Free Tier)
*   **Azure Key Vault**: `kv-smart-factory`
*   **Azure SignalR Service**: `signalr-smart-factory` (Free_F1 Tier)
*   **Azure Logic App Workflow**: `logic-smart-factory-notifications`
*   **Azure Service Bus**: `sb-smart-factory` (Basic Tier for Event Bus)

## 5. Backend Deployment (Kubernetes)

The microservices are containerized and deployed to a Kubernetes cluster (e.g., Azure Kubernetes Service - AKS).

### Step 5.1: Apply Kubernetes Manifests

The deployment is managed via the manifests in the `deploy/k8s/` directory. These manifests are ordered for correct application:

1.  **Namespace and Secrets**: Create the environment and secure credentials.
2.  **MySQL**: Deploy the database (if using a cluster-local database).
3.  **Microservices**: Deploy the 5 microservices.

```bash
# 1. Create Namespace and Secrets
kubectl apply -f deploy/k8s/00-namespace.yaml
kubectl apply -f deploy/k8s/01-secrets.yaml

# 2. Deploy MySQL (Optional: Skip if using a managed cloud database)
kubectl apply -f deploy/k8s/02-mysql.yaml

# 3. Deploy Microservices
kubectl apply -f deploy/k8s/device-service.yaml
kubectl apply -f deploy/k8s/identity-service.yaml
kubectl apply -f deploy/k8s/notification-service.yaml
kubectl apply -f deploy/k8s/analytics-service.yaml
kubectl apply -f deploy/k8s/telemetry-service.yaml
```

### Step 5.2: CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/main.yml`) automatically handles the build, test, and container image push to your configured Azure Container Registry (ACR) upon every push to the `main` branch. Ensure your ACR credentials (`ACR_USERNAME` and `ACR_PASSWORD`) are set up as GitHub Secrets.

## 6. Frontend Integration Guide (React App)

The frontend application (e.g., a React SPA) needs to be configured to interact with the deployed backend services for authentication, API calls, and real-time data.

### 6.1. Authentication Flow

The frontend must use the **Identity Service** for user authentication via **OAuth 2.0 / OpenID Connect (OIDC)** flow.

1.  **User Initiates Login**: The user clicks a login button in the React app.
2.  **Redirect to Identity Service**: The app redirects the user's browser to the Identity Service's authorization endpoint (e.g., Microsoft Entra ID login page).
3.  **Token Acquisition**: Upon successful login, the Identity Service redirects the user back to the React app with an **ID Token** (for user info) and an **Access Token** (JWT for API calls).
4.  **API Calls**: The React app stores the Access Token securely and includes it in the `Authorization: Bearer <token>` header for all subsequent RESTful API calls to the microservices.

### 6.2. Real-time Telemetry with SignalR

The frontend connects directly to the **Azure SignalR Service** endpoint to receive live telemetry updates from the `TelemetryService`.

1.  **Install SignalR Client**:
    ```bash
    npm install @microsoft/signalr
    ```
2.  **Connect to Hub**: Establish a connection to the `TelemetryHub` and `AlertsHub`.

```javascript
// Example: Connecting to the SignalR Hub in a React component
import * as signalR from "@microsoft/signalr";

// Replace with your actual SignalR endpoint
const SIGNALR_URL = "https://<your-signalr-service-url>/telemetryhub"; 

const connection = new signalR.HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
        // Pass the JWT Access Token for authorization
        accessTokenFactory: () => localStorage.getItem('accessToken') 
    })
    .withAutomaticReconnect()
    .build();

connection.start().then(() => {
    console.log("Connected to SignalR Telemetry Hub.");
    
    // Subscribe to the message event
    connection.on("ReceiveTelemetryUpdate", (data) => {
        // Update React state or Redux store with new telemetry data
        console.log("New telemetry:", data);
    });
    
    connection.on("ReceiveAlert", (alert) => {
        // Display a real-time notification to the user
        console.warn("New Alert:", alert);
    });
    
}).catch(err => console.error("SignalR Connection Error: ", err.toString()));
```

## 7. API Documentation

The backend services expose RESTful APIs documented using **Swagger/OpenAPI**.

### 7.1 Accessing Swagger UI

Once deployed, you can access the interactive API documentation at the following endpoints:
*   **Device Service**: `http://<k8s-ingress-ip>/device-service/swagger`
*   **Identity Service**: `http://<k8s-ingress-ip>/identity-service/swagger`
*   **Notification Service**: `http://<k8s-ingress-ip>/notification-service/swagger`

For a high-level overview of all APIs, refer to the [API-SPEC.md](./API-SPEC.md).

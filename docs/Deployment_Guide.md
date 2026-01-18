# Smart Factory IoT Backend Deployment Guide

## 1. Introduction and Architecture Overview

This document provides a comprehensive, step-by-step guide for deploying the **Smart Factory IoT Backend** built on a **.NET Microservices Architecture**. The system is designed for real-time industrial monitoring, integrating edge devices with Azure cloud services.

The architecture adheres to **Domain-Driven Design (DDD)** and **S.O.L.I.D. Principles**, utilizing containerization for portability and scalability.

| Component Layer | Key Technologies | Purpose |
| :--- | :--- | :--- |
| **Edge Layer** | Raspberry Pi 5, ESP-32 Wrover, MQTT/AMQP | Data acquisition and gateway functionality. |
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

## 3. Cloud Infrastructure Setup (Terraform)

The core Azure resources are provisioned using the provided Terraform configuration (`deploy/terraform/main.tf`).

### Step 3.1: Initialize and Plan

Navigate to the Terraform directory and initialize the workspace.

\`\`\`bash
cd smart-factory-backend/deploy/terraform
terraform init
\`\`\`

Review the plan to ensure the correct resources will be created. **Note**: You must replace `YOUR_TENANT_ID` in `main.tf` with your Azure Active Directory Tenant ID.

\`\`\`bash
terraform plan -var="tenant_id=<YOUR_TENANT_ID>"
\`\`\`

### Step 3.2: Apply Configuration

Apply the configuration to create the resources.

\`\`\`bash
terraform apply -var="tenant_id=<YOUR_TENANT_ID>"
\`\`\`

This will provision the following **free-tier** Azure services:
*   **Resource Group**: `rg-smart-factory`
*   **Azure IoT Hub**: `iothub-smart-factory` (F1 Free Tier)
*   **Azure Key Vault**: `kv-smart-factory`
*   **Azure SignalR Service**: `signalr-smart-factory` (Free_F1 Tier)
*   **Azure Logic App Workflow**: `logic-smart-factory-notifications` (Requires manual design for email/Teams integration)
*   **Azure Service Bus**: `sb-smart-factory` (Basic Tier for Event Bus)

## 4. Backend Deployment (CI/CD)

The microservices are containerized and deployed to a Kubernetes cluster (e.g., Azure Kubernetes Service - AKS).

### Step 4.1: Build and Containerize

The `src/Services/DeviceService/Dockerfile` provides the build steps for the Device Management Service. You will need to create similar Dockerfiles for the **IdentityService** and **NotificationService**.

1.  **Build the Docker Image**:
    \`\`\`bash
    docker build -t smartfactory.azurecr.io/identity-service:v1.0.0 ../../src/Services/IdentityService
    docker build -t smartfactory.azurecr.io/notification-service:v1.0.0 ../../src/Services/NotificationService
    \`\`\`
2.  **Push to Azure Container Registry (ACR)**:
    \`\`\`bash
    # Assuming you have an ACR named 'smartfactory'
    az acr login --name smartfactory
    docker push smartfactory.azurecr.io/identity-service:v1.0.0
    docker push smartfactory.azurecr.io/notification-service:v1.0.0
    \`\`\`

### Step 4.2: Deploy to Kubernetes

You will need to create Kubernetes deployment manifests for the new services, similar to `deploy/k8s/device-service.yaml`.

### Step 4.3: CI/CD Pipeline Template

The provided GitHub Actions workflow (`.github/workflows/main.yml`) is a template for your CI/CD process. You will need to extend the build and push steps to include the new services.

## 5. Service-Specific Configuration

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

## 6. Edge Device Configuration (No Change)

... (Sections 5.1 and 5.2 remain the same)

## 7. API Documentation (No Change)

... (Section 6 remains the same)

## 8. Next Steps

1.  **Database**: Provision and configure your Aiven MySQL instance and update the connection string in the Kubernetes secrets (`db-secrets`).
2.  **Authentication**: Complete the Microsoft Entra ID application registration and ensure all services are configured to use the Identity Service for JWT validation.
3.  **Frontend Integration**: Connect your existing React frontend to the deployed API Gateway (Azure APIM) and the SignalR Hub Service.

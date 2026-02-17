# Architecture Overview: Smart Factory IoT Backend

The **Smart Factory IoT Backend** is built upon a robust, scalable, and maintainable **Microservices Architecture** following the principles of **Domain-Driven Design (DDD)** and **S.O.L.I.D.** (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion). This design ensures high cohesion within services and low coupling between them, facilitating independent deployment and scaling.

## 1. Architectural Principles

### 1.1. Domain-Driven Design (DDD)
The system is decomposed into five core microservices, each corresponding to a distinct **Bounded Context** within the Smart Factory domain:

| Service | Bounded Context | Core Responsibility |
| :--- | :--- | :--- |
| **IdentityService** | Identity & Access Management | User authentication (Microsoft Entra ID) and authorization. |
| **DeviceService** | Device Management | CRUD operations for industrial device metadata, status, and lifecycle. |
| **TelemetryService** | Telemetry Ingestion & Persistence | Ingesting data from Azure IoT Hub, persisting raw data, and broadcasting real-time updates via SignalR. |
| **AnalyticsService** | Operational Analytics | Calculating key metrics like Overall Equipment Effectiveness (OEE) and detecting anomalies. |
| **NotificationService** | Alerting & Communication | Triggering alerts and sending notifications (email, Logic Apps) based on events. |

### 1.2. S.O.L.I.D. Principles Implementation

| Principle | Implementation in Architecture |
| :--- | :--- |
| **Single Responsibility Principle (SRP)** | Each microservice is responsible for a single, well-defined business capability (e.g., `DeviceService` only manages devices, not identity or analytics). |
| **Open/Closed Principle (OCP)** | Services are open for extension (e.g., adding a new notification channel) but closed for modification (existing core logic remains untouched). |
| **Liskov Substitution Principle (LSP)** | Interfaces and base classes are used extensively (e.g., `EventBus` abstraction) ensuring that derived types can be substituted without altering the correctness of the program. |
| **Interface Segregation Principle (ISP)** | Services communicate via small, client-specific interfaces (e.g., REST APIs for synchronous calls, Event Bus for asynchronous events). |
| **Dependency Inversion Principle (DIP)** | Dependencies on external resources (databases, message brokers) are abstracted using interfaces and injected via Dependency Injection (DI), allowing for easy substitution (e.g., using an in-memory database for testing). |

## 2. Communication and Data Flow

The system utilizes a hybrid communication model:

| Communication Type | Mechanism | Purpose |
| :--- | :--- | :--- |
| **Synchronous** | RESTful APIs (HTTP/S) | Used for direct, request-response operations (e.g., `DeviceService` API calls). |
| **Asynchronous** | Azure Service Bus (EventBus) | Used for event-driven communication between microservices (e.g., `TelemetryService` publishes a `NewTelemetryEvent` which `AnalyticsService` consumes). |
| **Real-time** | Azure SignalR Service | Used to push live data updates (telemetry, alerts) from the backend to the connected frontend clients. |

### 2.1. Data Persistence

Each service maintains its own data store to enforce **data ownership** (SRP). The primary data store is **PostgreSQL**, managed via Entity Framework Core (EF Core).

*   **Local Development**: A local PostgreSQL container is used via `docker-compose`.
*   **Production**: A managed service like Aiven PostgreSQL or Azure Database for PostgreSQL is used, with connection details managed securely via Kubernetes Secrets.

## 3. Deployment Architecture

The entire system is designed for cloud-native deployment, specifically targeting **Azure Kubernetes Service (AKS)**.

| Component | Technology | Role in Deployment |
| :--- | :--- | :--- |
| **Containerization** | Docker | Multi-stage Dockerfiles for optimized, secure images (non-root user, health checks). |
| **Orchestration** | Kubernetes (K8s) | Manages deployment, scaling (HPA), self-healing (probes), and service discovery. |
| **Configuration** | K8s ConfigMaps & Secrets | Stores non-sensitive and sensitive configuration separately. |
| **CI/CD** | GitHub Actions | Automates build, test, and push to Azure Container Registry (ACR). |
| **Infrastructure** | Terraform | Provisions all necessary Azure cloud resources (IoT Hub, Key Vault, SignalR, AKS). |

The Kubernetes manifests (`deploy/k8s/`) are structured to include:
*   **Namespace and ConfigMap**: Global configuration and isolation.
*   **Secrets**: Secure storage for connection strings and credentials.
*   **Deployment**: Defines the desired state (replicas, resource limits, security context).
*   **Service**: Provides stable internal cluster IP for service-to-service communication.
*   **Horizontal Pod Autoscaler (HPA)**: Ensures the system scales dynamically based on CPU and Memory utilization.

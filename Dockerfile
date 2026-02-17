# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and restore all projects
COPY ["src/SmartFactory.sln", "src/"]
COPY ["src/BuildingBlocks/EventBus/EventBus.csproj", "src/BuildingBlocks/EventBus/"]
COPY ["src/Services/AnalyticsService/AnalyticsService.csproj", "src/Services/AnalyticsService/"]
COPY ["src/Services/DeviceService/DeviceService.csproj", "src/Services/DeviceService/"]
COPY ["src/Services/IdentityService/IdentityService.csproj", "src/Services/IdentityService/"]
COPY ["src/Services/NotificationService/NotificationService.csproj", "src/Services/NotificationService/"]
COPY ["src/Services/TelemetryService/TelemetryService.csproj", "src/Services/TelemetryService/"]
COPY ["src/SmartFactory.Tests/SmartFactory.Tests.csproj", "src/SmartFactory.Tests/"]

RUN dotnet restore "src/SmartFactory.sln"

# Copy the entire source code
COPY . .

# Build and publish all services so runtime can select one via env var.
RUN dotnet publish "src/Services/DeviceService/DeviceService.csproj" -c Release -o /app/publish/DeviceService && \
    dotnet publish "src/Services/IdentityService/IdentityService.csproj" -c Release -o /app/publish/IdentityService && \
    dotnet publish "src/Services/NotificationService/NotificationService.csproj" -c Release -o /app/publish/NotificationService && \
    dotnet publish "src/Services/AnalyticsService/AnalyticsService.csproj" -c Release -o /app/publish/AnalyticsService && \
    dotnet publish "src/Services/TelemetryService/TelemetryService.csproj" -c Release -o /app/publish/TelemetryService

# Use the runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Select service at runtime (Render env var).
# Valid values: DeviceService, IdentityService, NotificationService, AnalyticsService, TelemetryService
ENV SERVICE_NAME=DeviceService
ENTRYPOINT ["sh", "-c", "dotnet /app/${SERVICE_NAME}/${SERVICE_NAME}.dll"]

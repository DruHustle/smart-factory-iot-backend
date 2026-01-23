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

# Build and publish each service
# We use an argument to specify which service to build
ARG SERVICE_NAME
RUN dotnet publish "src/Services/${SERVICE_NAME}/${SERVICE_NAME}.csproj" -c Release -o /app/publish

# Use the runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The entrypoint will be set dynamically or overridden in docker-compose/k8s
# But we can provide a default based on the SERVICE_NAME
ARG SERVICE_NAME
ENV SERVICE_DLL=${SERVICE_NAME}.dll
ENTRYPOINT ["sh", "-c", "dotnet $SERVICE_DLL"]

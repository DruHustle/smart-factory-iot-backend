FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution and project files for restoration
COPY ["src/SmartFactory.sln", "src/"]
COPY ["src/BuildingBlocks/EventBus/EventBus.csproj", "src/BuildingBlocks/EventBus/"]
COPY ["src/Services/NotificationService/NotificationService.csproj", "src/Services/NotificationService/"]

# Restore dependencies
RUN dotnet restore "src/Services/NotificationService/NotificationService.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the service
WORKDIR "/src/src/Services/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]

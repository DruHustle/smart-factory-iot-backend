FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution and project files for restoration
COPY ["src/SmartFactory.sln", "src/"]
COPY ["src/BuildingBlocks/EventBus/EventBus.csproj", "src/BuildingBlocks/EventBus/"]
COPY ["src/Services/AnalyticsService/AnalyticsService.csproj", "src/Services/AnalyticsService/"]

# Restore dependencies
RUN dotnet restore "src/Services/AnalyticsService/AnalyticsService.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the service
WORKDIR "/src/src/Services/AnalyticsService"
RUN dotnet build "AnalyticsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AnalyticsService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AnalyticsService.dll"]

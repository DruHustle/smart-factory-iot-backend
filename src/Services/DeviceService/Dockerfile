FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
# Note: The context for this Dockerfile should be the 'src' directory
COPY ["Services/DeviceService/DeviceService.csproj", "Services/DeviceService/"]
RUN dotnet restore "Services/DeviceService/DeviceService.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/Services/DeviceService"

# Build the project
RUN dotnet build "DeviceService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeviceService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeviceService.dll"]

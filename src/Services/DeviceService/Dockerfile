FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
# The context is 'src/Services/DeviceService', so the csproj is in the current directory
COPY ["DeviceService.csproj", "."]
RUN dotnet restore "DeviceService.csproj"

# Copy the rest of the source code for this service
COPY . .

# Build the project
RUN dotnet build "DeviceService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeviceService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeviceService.dll"]

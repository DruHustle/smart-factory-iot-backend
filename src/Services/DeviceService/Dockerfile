FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DeviceService.csproj", "Services/DeviceService/"]
RUN dotnet restore "Services/DeviceService/DeviceService.csproj"
COPY . .
WORKDIR "/src/Services/DeviceService"
RUN dotnet build "DeviceService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeviceService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeviceService.dll"]

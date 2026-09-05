# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/InventoryApi.Api/InventoryApi.Api.csproj", "Api/"]
COPY ["src/InventoryApi.Domain/InventoryApi.Domain.csproj", "Domain/"]
COPY ["src/InventoryApi.Infrastructure/InventoryApi.Infrastructure.csproj", "Infrastructure/"]
RUN dotnet restore "Api/InventoryApi.Api.csproj"

COPY . .
WORKDIR "/src/Api"
RUN dotnet publish "InventoryApi.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0

USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryApi.Api.dll"]

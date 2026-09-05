# 📦 High-Performance Inventory & Stock Movement API

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-316192?style=flat-square&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)](docker-compose.yml)
[![Tests: xUnit](https://img.shields.io/badge/Tests-xUnit%20%287%20Passed%29-brightgreen?style=flat-square)](tests/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

A high-throughput, enterprise Inventory Management REST API engineered with **ASP.NET Core 8 Minimal APIs**, **Entity Framework Core**, **PostgreSQL**, and **Redis Distributed Caching**.

---

## 🏛️ Architecture & Vertical Slices

```
InventoryApi/
├── InventoryApi.sln                         # .NET 8 Solution File
├── src/
│   ├── InventoryApi.Domain/                 # Pure Business Domain
│   │   └── Entities.cs                      # BaseEntity, Product, Category, Supplier, StockMovement
│   │
│   ├── InventoryApi.Infrastructure/         # Data Access & External Adapters
│   │   ├── InventoryDbContext.cs            # PostgreSQL EF Core Fluent API, indexes, seed data
│   │   └── Services/                        # CacheService (DistributedCache for Redis / Memory)
│   │
│   └── InventoryApi.Api/                    # Minimal APIs Presentation Layer
│       ├── Endpoints/                       # Vertical Slice Endpoint Mappings
│       │   ├── ProductEndpoints.cs          # Paginated CRUD, Low-stock alerts, Stock recording
│       │   ├── CategoryEndpoints.cs         # Category taxonomy with active product counts
│       │   ├── SupplierEndpoints.cs         # Supplier rating & vendor catalog management
│       │   └── StockMovementEndpoints.cs    # Historical audit trail of all warehouse movements
│       ├── Program.cs                       # DI, Swagger OpenAPI, Distributed Caching
│       ├── Dockerfile                       # Multi-stage production container build
│       └── appsettings.json
│
├── tests/
│   └── InventoryApi.UnitTests/              # Automated Test Suite
│       └── ProductDomainTests.cs            # 7 xUnit tests (profit margins, low-stock flags, audit math)
│
└── docker-compose.yml                       # Containerized API + PostgreSQL 16 + Redis
```

---

## 🚀 Key Engineering Features

- **Vertical Slice Architecture**: Endpoints are organized by feature modules rather than horizontal controller layers.
- **Complete Audit Trail**: Every stock change (Inbound, Outbound, Adjustment, Return) is permanently recorded with previous & new stock snapshots and reference numbers.
- **Automated Low-Stock Thresholds**: Instant query filtering for items falling below safety reorder levels.
- **Distributed Caching (Cache-Aside)**: Redis caching layer for read-heavy catalog endpoints.
- **Automated Testing**: 7 xUnit tests passing with 100% success covering domain calculations and business constraints.

---

## 🛠️ Quick Start

### 1. Run with Docker Compose
```bash
docker-compose up -d
# Swagger UI available at: http://localhost:5050/swagger
```

### 2. Run Local .NET CLI
```bash
# Clone & navigate
cd inventory-api

# Run automated tests
dotnet test InventoryApi.sln

# Run API
cd src/InventoryApi.Api
dotnet run
```

---

## 📄 License
MIT License — Copyright (c) 2026 Pham Van Minh (Minh Lap).

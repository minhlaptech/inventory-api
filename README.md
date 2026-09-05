# 📦 Inventory Management API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-316192?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7.x-DC382D?logo=redis)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A RESTful inventory management API built with **ASP.NET Core 8 Minimal APIs**. Features product CRUD, stock movement tracking with full audit trail, supplier management, low-stock alerts, and Redis caching.

---

## 🌟 Features

- **Product Management** — Full CRUD with SKU tracking, pricing (cost/unit), and profit margin calculation
- **Stock Movement Tracking** — Every stock change (inbound, outbound, adjustment, return) is recorded as an immutable audit entry
- **Low-Stock Alerts** — Automatic detection of products below their reorder threshold with severity levels
- **Supplier Management** — Track suppliers with performance ratings
- **Category Organization** — Hierarchical product categorization with slug-based URLs
- **Redis Caching** — Distributed cache for frequently-accessed catalog data
- **Pagination & Search** — Server-side pagination with full-text search and category filtering

---

## 🛠️ Tech Stack

| Layer | Technologies |
|---|---|
| **API** | ASP.NET Core 8 **Minimal APIs** |
| **Language** | C# 12 |
| **ORM** | Entity Framework Core 8 (Code-First, Fluent API) |
| **Database** | PostgreSQL 16 |
| **Caching** | Redis 7 (IDistributedCache) |
| **Auth** | API Key + JWT Bearer |
| **Testing** | xUnit + WebApplicationFactory |
| **Docs** | Swagger / OpenAPI 3.0 |
| **DevOps** | Docker, Docker Compose |

---

## 📁 Project Structure

```
inventory-api/
├── src/
│   ├── InventoryApi.Domain/                # Pure domain models, zero dependencies
│   │   └── Entities.cs                     # Product, Category, Supplier, StockMovement
│   │
│   ├── InventoryApi.Infrastructure/        # Data access layer
│   │   └── InventoryDbContext.cs           # EF Core context, Fluent API, seed data
│   │
│   └── InventoryApi.Api/                   # HTTP entry point (Minimal APIs)
│       └── Endpoints/
│           └── ProductEndpoints.cs         # Product CRUD, stock movements, alerts
│
├── tests/
│   └── InventoryApi.Tests/                 # Integration tests
│
├── docker-compose.yml
├── README.md
└── LICENSE
```

---

## 📡 API Endpoints

### Products
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/products` | Paginated product list (search, category filter, low-stock filter) |
| `GET` | `/api/v1/products/{id}` | Product detail with recent stock movements |
| `POST` | `/api/v1/products` | Create product with optional initial stock |
| `POST` | `/api/v1/products/{id}/stock` | Record stock movement (inbound/outbound/adjustment/return) |
| `GET` | `/api/v1/products/alerts/low-stock` | Products below reorder threshold |

### Example: Record Stock Movement
```bash
curl -X POST "http://localhost:5001/api/v1/products/{id}/stock" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "Inbound",
    "quantity": 50,
    "referenceNumber": "PO-2024-0042",
    "notes": "Monthly restocking from supplier",
    "performedBy": "warehouse-admin"
  }'
```

### Response
```json
{
  "movementId": "...",
  "productId": "...",
  "previousStock": 15,
  "newStock": 65,
  "isLowStock": false
}
```

---

## 🏗️ Architecture Highlights

### Minimal APIs Pattern
Unlike traditional MVC controllers, this project uses .NET 8 **Minimal APIs** with endpoint grouping for cleaner, more concise route definitions.

### Stock Movement Audit Trail
Every stock change creates an immutable `StockMovement` record capturing:
- Previous stock → New stock
- Movement type (inbound/outbound/adjustment/return)
- Reference number (PO, invoice, etc.)
- Performer identity and timestamp

### Computed Properties
Products expose calculated fields like `IsLowStock` and `ProfitMargin` without storing them in the database.

---

## 🚀 Quick Start

### Docker
```bash
docker compose up -d
# API: http://localhost:5001
# Swagger: http://localhost:5001/swagger
```

### Local
```bash
cd src/InventoryApi.Api
dotnet restore
dotnet ef database update
dotnet run
```

---

## 📄 License
MIT License — see [LICENSE](LICENSE) for details.

**Author:** Minh Lap (Pham Van Minh)  
🔗 [GitHub](https://github.com/minhlaptech) · [LinkedIn](https://www.linkedin.com/in/minhlaptech/)

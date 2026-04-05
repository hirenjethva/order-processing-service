# Order Processing Service

ASP.NET Core API for creating and tracking orders, with product catalog, Redis caching, MongoDB persistence, and RabbitMQ event publishing.

## Quick start (Docker)

From the repository root:

```bash
docker compose up --build
```

- **HTTP API:** [http://localhost:8080](http://localhost:8080) (maps host `8080` → container port `80`)
- **Swagger UI (Development):** [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **RabbitMQ management UI:** [http://localhost:15672](http://localhost:15672) (default `guest` / `guest`)
- **MongoDB:** `localhost:27017`
- **Redis:** `localhost:6379`

The API waits for MongoDB, Redis, and RabbitMQ to report healthy before starting. On first run, sample products are seeded into MongoDB if the `products` collection is empty.

## API endpoints

Base URL when using Docker: `http://localhost:8080`

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/products` | List all products (cache-aside via Redis, then MongoDB). |
| `GET` | `/api/products/{id}` | Get one product by id (`404` if missing). |
| `POST` | `/api/orders` | Create an order (validates lines, reserves stock, persists order, publishes `order.created`). |
| `GET` | `/api/orders/{id}` | Get an order by id (`404` if missing). |
| `PATCH` | `/api/orders/{id}/status` | Update order status (body: `{ "status": "<OrderStatus>" }`; invalid transitions return `409`). |

Order-related errors map to HTTP status codes (for example: validation `400`, product not found `404`, insufficient stock or invalid status transition `409`) via centralized problem details mapping.

## Design decisions

### Stock reservation strategy

Stock is decremented **atomically** in MongoDB using a single `findOneAndUpdate`: the update runs only when `StockQuantity >= requestedQuantity`, and returns the updated document (or nothing if the condition fails). That avoids check-then-act races for a single product line.

When an order contains multiple lines, reservations are applied **sequentially**. If any line fails, previously reserved quantities for that request are **released** in reverse order (compensating transactions) before the API reports failure.

If `CreateAsync` throws after stock was reserved, reservations are also released so inventory is not left inconsistent.

### State machine approach

Order status changes are governed by a dedicated `OrderStatusTransitionService`: allowed transitions are explicit (forward flow `Pending → Confirmed → Processing → Shipped → Delivered`, with **cancellation** only from `Pending` or `Confirmed`). Terminal states (`Delivered`, `Cancelled`) have no outgoing transitions. Invalid transitions surface as typed domain errors and map to HTTP `409 Conflict`, not ad hoc controller logic.

### Caching strategy

Product reads use **cache-aside**: `GET` all products uses key `products:all`; `GET` by id uses `products:{id}`. Entries are stored in Redis with a **5-minute TTL** after a database load.

When an order is created successfully, product-related cache entries are **invalidated** (per-product keys and a pattern covering list entries) so catalog views do not show stale stock after a sale.

## Running tests

Unit tests live under `tests/OrderProcessingService.Tests` and use **xUnit**, **Moq**, and **FluentAssertions**. They mock repositories, cache, and messaging—no Docker services required.

```bash
dotnet test tests/OrderProcessingService.Tests/OrderProcessingService.Tests.csproj
```

From the solution root:

```bash
dotnet test OrderProcessingService.sln
```

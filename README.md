# ProVantage

A full-stack enterprise procurement platform. It covers the entire procurement lifecycle — from vendor onboarding through requisitions, approvals, purchase orders, goods receipts, invoice matching, contracts, and budget tracking — with real-time notifications and a full audit trail.

Built as a portfolio project to demonstrate production-grade architecture patterns.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)
![Angular 17](https://img.shields.io/badge/Angular-17-DD0031?style=for-the-badge&logo=angular)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Azure%20Edge-CC2927?style=for-the-badge&logo=microsoftsqlserver)
![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?style=for-the-badge&logo=redis)
![SignalR](https://img.shields.io/badge/SignalR-Realtime-0C7CD5?style=for-the-badge)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker)

---

## What it does

- **Vendors** — onboard vendors, manage contacts, track approval status and ratings
- **Requisitions** — raise purchase requests with line items, route through approval workflows
- **Purchase Orders** — generate POs from approved requisitions, track delivery status
- **Goods Receipts** — record what was actually received against a PO
- **Invoice Matching** — three-way match invoices against POs and goods receipts, flag variances
- **Budgets** — allocate budgets by department and category, track committed vs spent
- **Contracts** — manage vendor contracts, get notified before expiry
- **Dashboard** — live KPI tiles, spend trend chart, pending approvals, recent activity
- **Notifications** — real-time alerts via SignalR when anything in the workflow changes
- **Audit Logs** — every create/update/delete is recorded with user, timestamp, and before/after values

---

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Angular 17 (standalone components, signals, Chart.js) |
| Backend | .NET 10 Web API (Clean Architecture, CQRS via MediatR) |
| Database | Azure SQL Edge (Docker) — EF Core 10 |
| Cache | Redis (Docker) — output cache + distributed cache |
| Real-time | SignalR WebSockets |
| Background jobs | Hangfire (contract expiry alerts, SLA escalation) |
| Logging | Seq structured logs (Docker) |
| Auth | JWT + refresh tokens, role-based access (Admin / Manager / Buyer) |
| Multi-tenancy | Every record is scoped to a tenant via EF Core global query filters |

---

## Architecture

```
ProVantage.Domain          — Entities, value objects, enums, domain events
ProVantage.Application     — CQRS handlers, validators, pipeline behaviors, DTOs
ProVantage.Infrastructure  — EF Core, Redis, SignalR hubs, Hangfire jobs, seed data
ProVantage.API             — Controllers, middleware, DI wiring, startup

client/provantage-ui/
  src/app/core/            — Auth service, guards, HTTP interceptor, shared services
  src/app/features/        — One folder per page (dashboard, vendors, requisitions, …)
  src/app/layout/          — Shell, sidebar, header
```

Requests flow: Angular → HTTP proxy → .NET API Controller → MediatR handler → EF Core → SQL Server.
Real-time updates flow back via SignalR WebSocket connections.

---

## Quick start

**Prerequisites:** Docker Desktop, .NET 10 SDK, Node 20+

### 1. Start infrastructure

```bash
docker compose up -d
```

This starts SQL Server (port 1433), Redis (port 6379), and Seq (port 5341).

### 2. Run the API

```bash
cd src/ProVantage.API
dotnet run
```

On first run in Development mode, the database is created and seeded automatically.

API is available at `http://localhost:5091`
Swagger UI: `http://localhost:5091/swagger`
Hangfire dashboard: `http://localhost:5091/hangfire`

### 3. Run the Angular app

```bash
cd client/provantage-ui
npm install
ng serve
```

App is available at `http://localhost:4200`

---

## Seed accounts

| Email | Password | Role |
|---|---|---|
| admin@acme.com | Admin123! | Admin |
| manager@acme.com | Admin123! | Manager |
| buyer@acme.com | Admin123! | Buyer |

The seed also creates sample vendors, contracts, requisitions, purchase orders, invoices, budgets, notifications, and audit log entries so every page has data to browse immediately.

---

## Key patterns

- **Clean Architecture** — strict dependency direction: Domain ← Application ← Infrastructure ← API
- **CQRS** — commands and queries are separate MediatR requests, handlers co-located with their request
- **Result pattern** — handlers return `Result<T>`, the base controller unwraps it to the right HTTP response
- **Multi-tenancy** — `TenantId` on every entity, resolved from the JWT claim, enforced by EF Core global filters
- **Pipeline behaviors** — validation (FluentValidation) and caching (`ICacheable`) wired as MediatR pipeline steps
- **Output caching** — read-heavy endpoints cached at the API layer, invalidated on write
- **Signals** — Angular state managed with Angular 17 signals instead of RxJS BehaviorSubjects

---

## Running in production mode

The Angular build is output-hashed and minified by default:

```bash
cd client/provantage-ui
ng build
```

Serve the `dist/provantage-ui/browser` folder from any static host or the .NET API's `wwwroot`.

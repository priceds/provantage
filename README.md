# ProVantage

**Enterprise Procurement & Vendor Management Platform**

ProVantage is a multi-tenant SaaS application that manages the full procurement lifecycle — from vendor onboarding and purchase requisitions through multi-level approvals, purchase orders, goods receipts, three-way invoice matching, and budget enforcement.

---

## Architecture

```mermaid
graph TB
    subgraph Client["🖥️ Angular 17 SPA"]
        UI_Auth["Login / Auth"]
        UI_Vendors["Vendor Management"]
        UI_Req["Requisitions"]
        UI_PO["Purchase Orders"]
        UI_INV["Invoices & 3-Way Match"]
        UI_BUD["Budgets"]
    end

    subgraph API["⚙️ .NET 10 Web API"]
        MW["Middleware<br/>(Tenant Resolution · CORS · Exception Handler)"]
        RL["Rate Limiting<br/>(Fixed / Sliding / Token Bucket)"]
        OC["Output Cache<br/>(Vendors 30s · Dashboard 1min)"]

        subgraph CQRS["MediatR Pipeline"]
            LOG["Logging Behavior"]
            VAL["Validation Behavior<br/>(FluentValidation)"]
            CACHE["Caching Behavior<br/>(Redis)"]
            CMD["Commands"]
            QRY["Queries"]
        end
    end

    subgraph Domain["📦 Domain Layer"]
        ENT["Entities<br/>(Vendor · PurchaseOrder · Invoice · GoodsReceipt · Budget…)"]
        VO["Value Objects<br/>(Money · Address · DateRange)"]
        EVT["Domain Events<br/>(RequisitionSubmitted · InvoiceMatched · BudgetThresholdExceeded)"]
    end

    subgraph Infra["🔧 Infrastructure"]
        EF["EF Core 10<br/>+ Global Filters<br/>(TenantId · SoftDelete)"]
        REDIS["Redis<br/>(Distributed Cache)"]
        TOKEN["Token Service<br/>(JWT + Refresh)"]
        SEED["Database Seeder"]
    end

    subgraph Storage["🗄️ Data Stores"]
        SQL[("SQL Server")]
        RDB[("Redis")]
    end

    Client -->|HTTPS · JWT| API
    API --> MW --> RL --> CQRS
    CQRS --> Domain
    Domain --> Infra
    EF --> SQL
    REDIS --> RDB
```

---

## How It Works — Core Flows

```mermaid
sequenceDiagram
    participant Buyer
    participant API
    participant ApprovalEngine
    participant Manager
    participant Vendor

    Buyer->>API: Create Requisition (draft)
    Buyer->>API: Submit Requisition
    API->>ApprovalEngine: Evaluate thresholds
    alt Amount ≤ $5K
        ApprovalEngine-->>API: Auto-approve
    else Amount ≤ $50K
        ApprovalEngine->>Manager: Single approval step
        Manager-->>API: Approve
    else Amount > $50K
        ApprovalEngine->>Manager: Step 1
        Manager-->>API: Approve
        ApprovalEngine->>Manager: Step 2 (Director/Admin)
        Manager-->>API: Approve
    end
    API->>API: Generate Purchase Order
    API->>Vendor: PO sent (status = Sent)
    Vendor-->>API: Goods Receipt recorded
    Vendor-->>API: Invoice submitted
    API->>API: Run Three-Way Match
    note over API: Invoice vs PO vs Goods Receipt<br/>Price variance % · Qty tolerance
    alt All lines within tolerance
        API-->>Buyer: Invoice MATCHED ✓
    else Discrepancy found
        API-->>Buyer: Invoice DISPUTED ⚠
    end
```

---

## Three-Way Match Engine

```mermaid
flowchart LR
    INV["Invoice\nLines"]
    PO["Purchase Order\nLines"]
    GR["Goods Receipts\n(per item)"]

    INV --> MATCH
    PO --> MATCH
    GR --> MATCH

    subgraph MATCH["Match Engine"]
        direction TB
        P["Price check\n|inv_price - po_price| / po_price ≤ tolerance%"]
        Q["Qty check\ninvoice_qty ≤ received_qty × (1 + tolerance%)"]
    end

    MATCH --> R{All lines\npass?}
    R -->|Yes| MATCHED["✅ Matched\nUpdate budget spend"]
    R -->|No| DISPUTED["⚠️ Disputed\nStore discrepancy notes\nper line"]
```

Tolerance percentages (`PriceVarianceTolerancePercent`, `QuantityVarianceTolerancePercent`) are configured per tenant.

---

## Solution Structure

```
ProVantage/
├── src/
│   ├── ProVantage.Domain/           # Entities, Value Objects, Enums, Domain Events
│   ├── ProVantage.Application/      # CQRS handlers, validators, pipeline behaviors, DTOs
│   ├── ProVantage.Infrastructure/   # EF Core, Redis, JWT, seeder, interceptors
│   └── ProVantage.API/              # Controllers, middleware, Program.cs
├── client/
│   └── provantage-ui/               # Angular 17 standalone SPA
│       └── src/app/
│           ├── core/                # Auth service, guards, interceptors
│           ├── features/            # One folder per page (login, vendors, requisitions…)
│           └── layout/              # Shell, sidebar, header
└── docker-compose.yml               # SQL Server + Redis + Seq
```

---

## Domain Model (key relationships)

```mermaid
erDiagram
    Tenant ||--o{ User : has
    Tenant ||--o{ Vendor : has
    Tenant ||--o{ BudgetAllocation : has

    Vendor ||--o{ VendorContact : has
    Vendor ||--o{ PurchaseOrder : receives
    Vendor ||--o{ Invoice : sends

    PurchaseRequisition ||--o{ RequisitionLineItem : contains
    PurchaseRequisition ||--o{ ApprovalWorkflow : triggers
    ApprovalWorkflow ||--o{ ApprovalStep : has

    PurchaseRequisition ||--o| PurchaseOrder : "converts to"
    PurchaseOrder ||--o{ OrderLineItem : contains
    PurchaseOrder ||--o{ GoodsReceipt : "received via"
    PurchaseOrder ||--o{ Invoice : "invoiced by"

    Invoice ||--o{ InvoiceLineItem : contains
```

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **API Framework** | .NET 10 Web API |
| **Architecture** | Clean Architecture — Domain / Application / Infrastructure / Presentation |
| **CQRS** | MediatR with pipeline behaviors (logging, validation, Redis caching) |
| **ORM** | Entity Framework Core 10 — SQL Server, Fluent API, global query filters |
| **Validation** | FluentValidation (auto-registered, runs in MediatR pipeline) |
| **Caching** | StackExchange.Redis + ASP.NET Output Cache |
| **Rate Limiting** | ASP.NET built-in (fixed window, sliding window, token bucket) |
| **Auth** | JWT Bearer + refresh tokens, RBAC (Admin / Manager / Buyer / Viewer) |
| **Logging** | Serilog → Seq |
| **Frontend** | Angular 17 Standalone — signals, reactive forms, lazy routes |
| **Styling** | Custom SCSS design system, glassmorphism dark mode |
| **Infrastructure** | Docker Compose — SQL Server (ARM64), Redis, Seq |

---

## Key Design Decisions

**Multi-tenancy** — every entity carries `TenantId`; EF Core global query filters enforce isolation at the ORM level so no query can accidentally cross tenant boundaries.

**Result\<T\> pattern** — handlers never throw for expected failures. Every command and query returns `Result` or `Result<T>` with an `IsSuccess` flag, error message, and HTTP status code, keeping controller code uniform.

**Money value object** — monetary amounts are always paired with a currency string (`Money(amount, currency)`). Arithmetic operations (`Add`, `Subtract`, `Multiply`) enforce same-currency checks at compile time.

**Pipeline behaviors** — cross-cutting concerns (logging, validation, caching) are wired as MediatR pipeline behaviors so individual handlers stay focused on business logic only.

**Soft delete + audit** — `IsDeleted` / `DeletedAt` handled by `SoftDeleteInterceptor`; `CreatedBy` / `ModifiedBy` / timestamps by `AuditableEntityInterceptor`. Neither requires any handler code.

---

## Running Locally

**Prerequisites:** .NET 10 SDK · Node.js 20+ · Docker Desktop

```bash
# 1 — Start infrastructure
docker compose up -d

# 2 — API  (http://localhost:5000 · Swagger at /swagger)
cd src/ProVantage.API
dotnet run

# 3 — Angular SPA  (http://localhost:4200)
cd client/provantage-ui
npm install && npm start
```

---

## API Overview

| Area | Endpoints |
|------|-----------|
| Auth | `POST` login · register · refresh-token · revoke-token |
| Vendors | `GET/POST` list+create · `GET/PUT` detail+update · `PATCH` status |
| Requisitions | `GET/POST` list+create · `GET` detail · `POST` submit / approve / reject |
| Purchase Orders | `GET/POST` list+create · `GET` detail · `PATCH` status |
| Goods Receipts | `POST` record · `GET` list by PO |
| Invoices | `GET/POST` list+create · `GET` detail · `POST /{id}/match` |
| Budgets | `GET` utilization · `POST` allocate |

---

## License

MIT

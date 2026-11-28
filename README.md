# Property Deals API

A production-style ASP.NET Core API for real estate transactions ("deals"), from listing to closing: properties,
deal participants with roles, documents, status changes and a full activity history. It will be deployed to AWS
with least-privilege access: IAM roles instead of static keys, temporary STS credentials, and every resource
created by Terraform.

## Architecture

Clean Architecture with four layers. Dependencies point inward only:

```
Api  ──►  Infrastructure  ──►  Application  ──►  Domain
```

| Layer | Contains |
|---|---|
| **Domain** | Entities, value objects (`Money`, `Address`, `Email`), domain events, domain exceptions with stable error codes. References nothing. |
| **Application** | Use cases (commands and queries) and the interfaces they need, such as `IAppDbContext`. References only Domain. |
| **Infrastructure** | EF Core with PostgreSQL, entity configurations, migrations, the transactional outbox, development seed data. |
| **Api** | HTTP endpoints, middleware and dependency injection setup. |

Business rules live inside the entities. For example, a deal moves only through its state machine:

```
Draft → Active → UnderContract → Closing → Closed
  (any status except Closed can move to Cancelled)
```

## Tech stack

- .NET 10, ASP.NET Core, EF Core 10
- PostgreSQL 17 in Docker Compose
- xUnit for tests
- Planned: MediatR, FluentValidation, JWT, Redis, RabbitMQ, Amazon S3, GitHub Actions, Terraform on AWS

## Getting started

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Docker Desktop](https://www.docker.com/products/docker-desktop/).

1. Create your local environment file and choose a password:
   ```
   cp .env.example .env
   ```
2. Start PostgreSQL (it listens on port **5433**):
   ```
   docker compose up -d
   ```
3. Restore the local tools (`dotnet-ef`):
   ```
   dotnet tool restore
   ```
4. Store the connection string in user secrets. The password must be the same as in `.env`:
   ```
   dotnet user-secrets set "ConnectionStrings:PropertyDeals" "Host=localhost;Port=5433;Database=property_deals;Username=property_deals_app;Password=<password from .env>" --project src/PropertyDeals.API
   ```
5. Create the database schema:
   ```
   dotnet ef database update --project src/PropertyDeals.Infrastructure --startup-project src/PropertyDeals.API
   ```
6. Run the API. In Development it fills an empty database with demo data (users, properties, deals in every status,
   documents and activity history):
   ```
   dotnet run --project src/PropertyDeals.API
   ```

To start again from an empty database: `docker compose down -v`, then repeat steps 2, 5 and 6.

## Running tests

```
dotnet test PropertyDeals.slnx
```

The domain rules are covered by fast unit tests that need no database.

## Status

- [x] Phase 0 – Setup
- [x] Phase 1 – Domain layer
- [x] Phase 2 – Persistence (EF Core, migrations, seed data)
- [ ] Phase 3 – Application layer and API
- [ ] Phase 4 – Authentication and authorization
- [ ] Phase 5 – Documents with S3
- [ ] Phase 6 – Cache, events and idempotency
- [ ] Phase 7 – Testing and CI
- [ ] Phase 8 – AWS infrastructure with Terraform
- [ ] Phase 9 – Deployment and polish

## Decisions

Every design decision, the alternative and the reason are recorded in [DECISIONS.md](DECISIONS.md).

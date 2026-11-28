# Decisions

Each entry: what was chosen, the alternative, and why.

## Phase 1 – Domain layer

### Rich domain model with private constructors and static `Create` methods
Entities have empty private constructors and are built only through `Create`, which validates input and sets state with object initializers. The alternative was public constructors or an anemic model with validation in services. With this design an invalid entity cannot exist, and EF Core can still use the private constructor.

### A deal's creator is always its listing agent
`Deal.Create` takes the listing agent's id and stores it as `CreatedBy`, and that agent is added as the first participant. The alternative was a separate creator who might not take part in the deal. Only agents create deals, and the agent who creates one is the one who lists it, so one id avoids a deal whose creator cannot see it.

### A property's price cannot change currency
`Property.ChangePrice` rejects a new price in a different currency. The alternative was to allow it and convert or simply overwrite. A currency change is almost always a data-entry mistake, and price-history events would compare amounts in different currencies, so the rule stays strict until a real use case appears.

### Rules that need other aggregates are enforced in handlers, not entities
`User.ChangeRole` does not know about deals, so it cannot block a role change for a user who is a participant in an open deal (rule 4a). That check belongs to the Application handler, which can query deals before calling `ChangeRole`. The alternative was loading deals into `User`, which would break aggregate boundaries.

### Exceptions per rule, each with a stable error code
Every broken rule throws its own exception (e.g. `InvalidDealStatusTransitionException`, code `deal.invalid_status_transition`), and plain input errors throw `DomainValidationException`. The alternative was one generic exception with a message. Stable codes let the API map errors to the right HTTP status (409, 400) and let clients rely on them; tests lock every code.

### Time is passed in, always UTC
Methods take `utcNow` instead of reading `DateTime.UtcNow`, and `Guard.EnsureUtc` rejects other kinds with `ArgumentException`. The alternative was reading the clock inside entities. Passing time in makes tests deterministic, and a non-UTC value is a programmer error, not user input, so it is not a domain validation error.

### S3 keys never contain the user's file name
Keys are `deals/{dealId}/{guid}{extension}` with a lower-case, allow-listed extension. The alternative was using the uploaded file name. User file names can collide, leak personal data, or contain path tricks; the original name is kept only as metadata.

### Domain events contain no secrets
`UserPasswordChangedEvent` carries only the user id, who changed it and when, never the hash. Events are written to the outbox and sent to RabbitMQ, so anything inside them leaves the database.

## Phase 2 – Persistence

### snake_case table and column names
`UseSnakeCaseNamingConvention` maps `ClosingDate` to `closing_date` and `Deals` to `deals`. The alternative was EF's default PascalCase names. PostgreSQL folds unquoted names to lower case, so PascalCase would force quotes around every name in raw SQL; snake_case is the PostgreSQL convention.

### Value objects as complex types
`Money` and `Address` are mapped with `ComplexProperty`, so their values become columns of the `properties` table (`price_amount`, `address_city`). The alternatives were owned types or separate tables. Value objects have no identity of their own, so they belong in their owner's row, and complex types keep value semantics without a hidden key.

### Enums stored as text
Enum columns store names such as `UnderContract`, limited to 32 characters. The alternative was storing integers. Text is readable in queries and logs, and reordering enum members can never silently change the meaning of existing rows.

### Delete behaviour follows aggregate boundaries
Only `participants → deals` cascades, because participants are part of the Deal aggregate. Every reference between aggregates (deal to property, document to deal, anything to user) is `Restrict`. The alternative was EF's default cascade on required relationships, which could delete history, such as activity logs, together with a deal.

### `DbSet` in the Application layer
`IAppDbContext` exposes `DbSet<T>`, so Application references EF Core. The alternative was a repository per aggregate. Handlers can then project queries straight to DTOs with `Select` and `AsNoTracking`, which the plan requires; a repository layer over EF would mostly forward calls.

### Outbox lives in Infrastructure
`OutboxMessage` moved from Domain to Infrastructure, and Application does not see it. The alternative was keeping it in Domain. The outbox is a delivery mechanism, not a business concept: Infrastructure will turn domain events into outbox rows when changes are saved.

### Migrations are applied by the CLI, never on application startup
Migrations run with `dotnet ef database update` locally, and later with a script or migration bundle in CI. The alternative was calling `Migrate()` when the API starts. That would require the app's database user to change the schema, while least privilege says the app user must not, and several instances starting at once could race.

### Development seed data goes through domain factories
`DevDataSeeder` builds users, properties, deals, documents and activity history with the same `Create` and state-machine methods the API uses, in one transaction, only in Development and only when the database is empty. The alternative was `HasData` in migrations, which needs fixed ids, bypasses domain rules and does not fit complex types. Seed data can therefore never break a business rule.

### SQL logging only in Development
SQL commands are logged at Information level, with parameter values, only in Development; production logs only EF warnings and errors. The alternative was the same logging everywhere. Parameter values include emails and password hashes, so they must never reach production logs.

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

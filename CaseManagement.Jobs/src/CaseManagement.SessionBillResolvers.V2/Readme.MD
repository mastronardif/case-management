
(Session, Case, ProjectionDefinition)
    ↓
Resolver
    ↓
BillingProjection


Context JSON
    +
Projection Definition JSON
    ↓
Projection Resolver
    ↓
BillingProjection

----------------------------
Clinical Document
    ↓
Projection Engine
    ↓
Billing Projection
    ↓
Billing Rules
    ↓
Invoice

---------
>dotnet run -- --workflow 43 --action projectorProcess
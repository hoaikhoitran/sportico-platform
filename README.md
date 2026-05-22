## System Design

Sportico follows a Clean Architecture approach to isolate business rules from infrastructure concerns.

### Architecture Layers

- API Layer: Handles HTTP transport, authentication, and request validation.
- Application Layer: Implements use cases and orchestrates domain workflows (feed retrieval, post creation, boost activation).
- Domain Layer: Encapsulates core business rules (Post, User, Engagement, Boost rules).
- Infrastructure Layer: Implements persistence, caching, and external integrations.

This separation ensures that ranking logic and feed rules remain independent of database or caching technologies.

---

## Feed Architecture (CQRS-based Read Model)

The feed system is designed as a read-optimized model due to high read traffic characteristics.

Instead of computing rankings at query time, Sportico uses a **precomputed feed projection model**:

### Write Path (Command Side)
- Posts are created and stored in the primary Post table.
- Engagement events (likes, views, comments) are stored as raw events.
- Boost (premium visibility) is stored as a separate state entity.

### Read Path (Query Side)
- A background process computes a **FeedItem projection**.
- This projection contains:
  - Post metadata
  - Engagement aggregates
  - Boost weight
  - Time decay score
  - Final ranking score

The feed API queries only the projection model, avoiding expensive runtime joins and aggregation.

This approach reduces read latency and allows ranking logic to evolve without affecting write performance.

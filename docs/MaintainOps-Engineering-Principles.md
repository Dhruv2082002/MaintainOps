# MaintainOps — Engineering Principles

This document defines **how** MaintainOps is built. **What** is built (domain, actors, business rules, scale envelope and sprint sequence) is defined in `MaintainOps-Product-Definition-v2.md`.

MaintainOps is a **learning project for building production-grade applications**, not a race to finish a product.

---

# Governing Principle

The application should be designed as a realistic production system from the beginning.

That means the first implementation of a flow should already be engineered with serious usage in mind. The project should not knowingly create shortcuts that only work for a tiny demo and then schedule a future "productionizing" phase to repair them.

At the same time, infrastructure should not be added merely because it might someday be useful.

The governing principle is:

> Build the simplest implementation that is structurally sound for a serious production application.

---

# Primary Learning Goal

The goal is to learn how to take a business workflow and engineer it properly.

The desired skill is:

```text
Understand the user flow
        ↓
Define business rules
        ↓
Design API and data behavior
        ↓
Evaluate production concerns
        ↓
Choose appropriate architecture
        ↓
Implement
        ↓
Test
        ↓
Observe
        ↓
Refine
```

The project is not intended to teach technologies in isolation.

It should not become:

```text
Learn PostgreSQL
Learn Redis
Learn SignalR
Learn Hangfire
Learn OpenTelemetry
```

Instead, technologies should appear naturally because a product workflow creates a real engineering need.

For example:

```text
Need durable relational data
→ PostgreSQL / EF Core

Need duplicate command protection
→ idempotency

Need conflict protection
→ optimistic concurrency

Need reliable post-commit processing
→ Outbox

Need work outside the HTTP request
→ background jobs

Need file storage
→ object storage

Need live browser updates
→ SignalR
```

---

# Technology Categories

## Foundational capabilities

Some concerns benefit almost every mature application and should exist before substantial domain work begins.

These belong to the application foundation.

Examples include:

- ASP.NET Core
- dependency injection
- configuration
- OpenAPI
- ProblemDetails
- validation conventions
- authentication infrastructure
- authorization infrastructure
- rate limiting
- request correlation
- structured logging
- distributed tracing
- metrics
- health checks
- cancellation conventions
- testing infrastructure
- frontend application shell
- frontend error handling conventions
- security baseline
- CI/CD
- containerization
- secret handling

The foundation should make future workflows automatically inherit these capabilities where possible.

---

## Requirement-driven capabilities

Other technologies should only be implemented when a workflow creates a concrete need.

Examples include:

- PostgreSQL
- EF Core
- database transactions
- optimistic concurrency
- idempotency keys
- HybridCache
- Redis
- Hangfire
- domain events
- Outbox
- S3-compatible object storage
- CDN
- SignalR
- Elasticsearch/OpenSearch
- external API resilience
- feature flags

These technologies must **not receive chronological roadmap stages of their own**.

There should never be a sprint called:

```text
Redis Sprint
SignalR Sprint
Outbox Sprint
Caching Sprint
```

A product flow introduces or expands them if required.

---

# Roadmap Structure

The roadmap should have only two chronological sections.

## Part 1 — Foundations

Build the reusable production application platform.

This should cover the cross-cutting behavior that future flows depend on.

Possible foundation work includes:

- repository and solution structure
- backend and frontend application shells
- API conventions
- validation
- ProblemDetails
- authentication
- authorization infrastructure
- rate limiting
- secure browser session handling
- logging
- correlation
- tracing
- metrics
- health checks
- testing harnesses
- Docker
- CI/CD
- frontend routing and API conventions

The foundation should be substantial enough that the first domain flow can be built properly.

It should not attempt to predict every future infrastructure requirement.

---

## Part 2 — Product Sprints

After the foundation, every sprint should represent a **complete user-visible workflow**.

The sprint sequence is defined in `MaintainOps-Product-Definition-v2.md`, section 10.

---

# Environments

- **Dev:** the API runs locally with `dotnet run`. Docker Compose runs local dependencies, starting with PostgreSQL, which Foundations introduces because ASP.NET Core Identity stores user accounts in it.
- **Prod:** one live environment on Azure App Service. It is the only deployed environment.
- **Infrastructure:** all Azure resources are defined in Bicep. Nothing is created by hand in the portal except a one-time bootstrap: the resource group and the identity GitHub Actions uses to deploy.
- **Delivery:** GitHub Actions builds, tests and deploys to App Service on push to main.

---

# How Each Sprint Must Be Designed

Every sprint begins with the user journey.

For example:

> An administrator manages sites.

The sprint is not:

```text
POST /sites
```

It is closer to:

```text
Open Sites
→ see existing sites
→ create site
→ view site
→ edit site
→ deactivate site
```

The backend and frontend should be designed together around that workflow.

---

# Mandatory Production Review for Every Flow

Every new endpoint, command, query, screen, or workflow should be evaluated against the same production concerns.

The important part is **considering each concern**, not automatically implementing every technique.

## Security

Ask:

- Who can perform this action?
- Which organization owns the data?
- Can one tenant access another tenant's data?
- Does this operation need a specific authorization policy?

## Validation

Ask:

- What input is structurally invalid?
- What input violates business rules?
- Which rules belong in request validation?
- Which rules belong in the domain?

## Persistence

Ask:

- What needs to be stored?
- What relationships exist?
- What database constraints belong at the data layer?
- What indexes support the access patterns?

## Transactions

Ask:

- Does the operation change multiple pieces of state?
- Must those changes succeed or fail together?
- What is the correct transactional boundary?

## Idempotency

Every state-changing operation should have an explicit duplicate-execution strategy.

Ask:

- What happens if the request is delivered twice?
- Can the client safely retry?
- Could duplicate execution create duplicate resources or side effects?
- Is the operation naturally idempotent?
- Is a database uniqueness constraint enough?
- Should an idempotency key be used?

Do not wait for a later "idempotency stage."

For example, `Create Site` should consider idempotency the first time it is designed.

## Concurrency

For every edit or state transition ask:

- What happens if two users modify this simultaneously?
- Is last-write-wins acceptable?
- Should stale changes fail?
- Does optimistic concurrency belong here?

## Reads and Caching

Caching must be considered when the read endpoint is first designed.

Ask:

- How frequently is this read?
- How frequently does the data change?
- How expensive is the query?
- What is the expected cardinality?
- Is the data tenant-specific or user-specific?
- Is stale data acceptable?
- How would invalidation work?
- Is a cache actually cheaper and simpler than querying the database?

The decision may be:

> Do not cache this.

That is a valid production decision.

Do not deliberately build an obviously cache-worthy endpoint badly just so caching can be added as a future lesson.

## Query Scalability

Assume meaningful load from the beginning.

Ask:

- Is the query bounded?
- Does the endpoint need pagination?
- Are only required columns selected?
- Could this cause N+1 queries?
- Are indexes aligned with the query?
- What happens if the table contains millions of records?

Avoid designs that knowingly fail once the application has real usage.

## Horizontal Scaling

Assume the application may eventually run on multiple instances.

Avoid designs that depend on:

- correctness-critical in-memory state
- a single process receiving every related request
- local-only synchronization
- unbounded in-process queues

This does not mean Redis or distributed infrastructure must be added immediately.

It means early decisions should not make horizontal scaling unnecessarily difficult.

## Reliability

Ask:

- What happens if the process stops midway?
- What happens if a dependency fails?
- Can the operation be retried?
- Is follow-up work allowed to disappear?
- Should any work move outside the HTTP request?

## Observability

Ask:

- What should appear in structured logs?
- What trace information is useful?
- What technical metric matters?
- Is there a useful business metric?
- Would an operator be able to diagnose failure?

## Audit

Ask:

- Is this merely operationally interesting?
- Or is this a sensitive business action that needs durable audit history?

Do not confuse application logs with audit records.

## Frontend Behavior

For every workflow consider:

- loading
- empty states
- errors
- retries
- stale data
- optimistic UI
- authorization-related UX
- responsive behavior
- accessibility

## Testing

Ask:

- Which business rules deserve unit tests?
- Which database behavior requires integration tests?
- Which frontend behavior needs component tests?
- Is the flow important enough for an end-to-end test?

---

# Assume Meaningful Production Load From the Start

The project should adopt a production mentality even if it never reaches very high traffic.

Do not intentionally create designs that fail at scale simply because the current dataset is small.

Good early assumptions include:

- API instances are stateless
- any instance may receive any request
- database queries are bounded
- collection endpoints consider pagination
- indexes follow access patterns
- asynchronous I/O accepts cancellation
- state-changing requests consider retries
- concurrent users are expected
- external dependencies can fail
- expensive or long-running work is identified
- large binary data uses appropriate storage
- failures are observable
- hot paths can eventually scale horizontally

This does **not** mean adding distributed systems everywhere.

It means avoiding obviously fragile designs.

For example:

```text
Indexed PostgreSQL query
```

may be entirely appropriate.

It is preferable to prematurely adding Redis if the database query is cheap.

The principle is:

> Do not over-engineer for hypothetical scale, but do not knowingly engineer only for toy scale.

---

# Existing Features Must Be Designed for Growth From the Start

The roadmap should expect existing workflows to evolve, but it should not intentionally implement them in a way that only works for a small application.

Assume meaningful load from the first design.

For example, Site reads should initially be designed with:

- tenant-scoped authorization
- bounded result sets
- pagination where the collection can grow
- projections instead of loading unnecessary columns
- indexes aligned with actual access patterns
- cancellation
- protection against N+1 queries
- a clear cacheability and invalidation decision
- behavior that remains correct across multiple application instances

The initial implementation may still query PostgreSQL directly. That is acceptable if the query is efficient, bounded, indexed, observable, and appropriate for the expected read volume.

Caching should not be added merely because the feature is old or because the roadmap has reached a caching stage. It should be introduced when the workload, latency requirements, cost, or availability goals justify it.

For example:

- asset forms reuse site information
- work-order forms reuse it
- filters reuse it
- navigation uses it
- traffic increases
- database load or latency becomes significant
- the data is sufficiently stable and cacheable

At that point, the caching decision can be revisited and implemented without redesigning the feature from scratch.

That is healthy evolution. The important distinction is:

> Start with a production-sound design, then add complexity when measured requirements justify it.

Similarly:

- concurrency rules may become stricter as more users edit the same records
- audit coverage may expand as sensitive workflows are introduced
- queries may need additional indexes as access patterns and data volume grow
- background processing may appear when work becomes too slow or unreliable for an HTTP request
- existing direct side effects may need an Outbox when reliable post-commit delivery becomes necessary
- an existing dashboard may gain live updates when polling no longer meets the product requirement
- PostgreSQL search may eventually become dedicated search when relevance, scale, or query complexity justifies it

The application should accumulate engineering maturity without accumulating deliberate toy-scale shortcuts.

The roadmap should therefore distinguish between:

```text
Production-safe from the beginning
```

and:

```text
Additional optimization or infrastructure justified by later evidence
```

It should not defer basic scalability properties such as bounded queries, appropriate indexes, statelessness, retry safety, concurrency consideration, or failure handling.

It may defer specialized infrastructure such as distributed caching, dedicated search, live-update transport, or background-job platforms when the current workflow does not require them.

The application should evolve through evidence and changing requirements, not through a predetermined technology sequence.

---

# How Each Sprint Is Run

No sprint is built in one go.

Every sprint goes through these steps:

## Step 1 — Product discussion

Explain:

- what the user is trying to accomplish
- the full workflow
- business rules
- edge cases

## Step 2 — Engineering design

Review:

- API
- persistence
- authorization
- validation
- queries
- transactions
- idempotency
- concurrency
- caching
- scalability
- reliability
- observability
- frontend behavior
- testing

## Step 3 — Decisions

Explicitly state which concerns:

- apply now
- are already handled by the foundation
- require new infrastructure
- do not apply
- are intentionally deferred because there is no current requirement

## Step 4 — Implementation

Implement incrementally.

Each part should be understood before moving on, rather than accepting one large solution all at once.

## Step 5 — Review

At the end of the sprint, be able to explain:

- what was built
- why it was designed that way
- which engineering problems were considered
- which technologies were used and why
- which technologies were rejected and why
- what happens under duplicate requests
- what happens under concurrent requests
- what happens under meaningful load
- what happens when dependencies fail
- how the flow is observed
- how the flow is tested

---

# The Core Rule for Building the Roadmap

A new roadmap should follow this rule:

> **Chronology belongs to product behavior. Architecture belongs to the design review of each product behavior.**

Therefore:

```text
Correct:
Sprint 7 — A manager assigns work and technicians see their queue

Incorrect:
Sprint 7 — Optimistic Concurrency

Correct:
Sprint 10 — A technician attaches photo evidence at completion

Incorrect:
Sprint 10 — S3

Correct:
Sprint 15 — A manager monitors live operations and SLA breach risk

Incorrect:
Sprint 15 — SignalR
```

The technology may naturally appear during those sprints, but the technology never defines the sprint.

---

# Final Objective

At the end of the learning journey, success is not:

> I used every technology on the original list.

Success is:

> I can take an unfamiliar business workflow and reason through how to build it correctly as part of a production-grade system.

The desired engineering habit is:

```text
Product requirement
        ↓
Complete user flow
        ↓
Business rules
        ↓
Production review
        ↓
Architecture decision
        ↓
Appropriate technology
        ↓
Implementation
        ↓
Testing and observation
        ↓
Evolution
```

MaintainOps is the vehicle for repeatedly practicing that process.

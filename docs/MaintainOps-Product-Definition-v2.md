# MaintainOps — Product Definition v2

This document defines **what** MaintainOps is. **How** it is built (foundations, the per-sprint
production review and the roadmap rules) is defined in `MaintainOps-Engineering-Principles.md`.

---

## 1. Domain

MaintainOps is a maintenance management system for **organizations that operate many similar
physical locations** — retail chains, restaurant groups, gyms, clinics, branch offices.

It is explicitly **not**:

- a manufacturing reliability system (no production lines, no OEE, no condition monitoring)
- a property management system (no tenants, no leases, no rent)
- a field service dispatch system (no route optimization, no customer billing)

Why this vertical: the people who report problems are **not** the people who fix them, which
makes the request/work-order split real rather than academic. Sites number in the hundreds,
which makes tenancy and site-scoped authorization non-trivial. Asset trees stay shallow, so
early sprints remain tractable.

---

## 2. Actors

| Actor | Scope | Can | Cannot |
|---|---|---|---|
| Requester (site staff) | Own site only | Submit requests, view own requests, comment on them | See work orders, see other sites, set authoritative priority |
| Technician | Sites they are assigned to | View assigned + unassigned queue for their sites, start/hold/complete work, attach evidence, log time | Assign work, change priority, close a work order, delete anything |
| Maintenance Manager | **Specific sites**, not whole org | Triage requests, create/assign/cancel work orders, approve closure, create PM plans | Manage org members, manage sites outside their scope |
| Org Admin | Whole org | Members, roles, sites, assets, org settings | Bypass audit; act outside own org |
| Auditor / Viewer | Whole org, read-only | Read everything including audit log | Any mutation |
| Platform Admin | Cross-tenant | Support operations on tenant metadata (suspend org, inspect quotas) | Read tenant business data without an explicit, audited, time-boxed grant |

**Load-bearing decision:** manager scope is **per-site**, not per-org. Authorization is therefore
`(user, role, site)` — not `(user, role)`. This is what makes the security review in every sprint
have real content.

**Platform Admin is the riskiest actor.** If it turns out to add nothing but cross-tenant data
exposure, cut it. Keeping it means every query needs a tenant-scoping strategy that a privileged
principal can deliberately widen, and every widening must be audited.

---

## 3. Core objects

`Organization`, `Site`, `Location`, `Asset`, `Request`, `WorkOrder`, `PMPlan`, `Comment`,
`Attachment`, `AuditEvent`, `Membership`.

### 3.1 Request and WorkOrder are separate objects

A **Request** is a report. It does not authorize work.
A **WorkOrder** is an authorization to spend labor.

Rules:

- Any Requester may submit a Request. It requires: site, description, self-declared urgency.
  Asset and location are optional — "water on the floor near the back door" is a valid request.
- A Manager triages: **Accept**, **Reject** (reason required), or **Duplicate** (must link to an
  existing Request or WorkOrder).
- Accepting either creates a new WorkOrder or attaches the Request to an existing open one.
- **Many Requests → one WorkOrder. One Request → at most one WorkOrder.**
- A WorkOrder may exist with **zero** Requests (PM-generated, or manager-initiated).
- The SLA clock starts at Request submission, not WorkOrder creation.
- Closing a WorkOrder closes all Requests attached to it and notifies each requester.

Request lifecycle: `Submitted → UnderReview → Accepted | Rejected | Duplicate`.
Terminal states are final; a rejected request cannot be re-triaged, only re-submitted.

### 3.2 Asset model

```
Site  →  Location  →  Asset  →  Component
```

- **Site**: a physical address. Belongs to org. Has timezone (needed for PM scheduling and SLA
  business hours).
- **Location**: an area within a site — floor, room, zone. One level only, no nesting.
- **Asset**: a maintainable thing. Belongs to a Location. Has an optional `parent_asset_id`,
  **depth-capped at one level** (Asset → Component). Deeper nesting is rejected at write time.
- Fields: `asset_code` (unique per organization), category, criticality (A/B/C), manufacturer,
  model, serial, install date, warranty expiry, status (`Active | Down | Retired`).

**Rule:** a WorkOrder targets **either** an Asset **or** a Location. Not everything broken is an
asset.

**Deactivation semantics (decide once, applies everywhere):** retiring an Asset suspends its PM
plans, blocks new WorkOrders, and leaves existing open WorkOrders alone. It never deletes history.
Same pattern for Site and Location.

---

## 4. WorkOrder state machine

| From | To | Who | Required data |
|---|---|---|---|
| — | `Open` | Manager, or PM generator | Site, priority, title, source (Request / PM / Manual) |
| `Open` | `Assigned` | Manager | Assignee must hold Technician role on that WorkOrder's site |
| `Assigned` | `Open` | Manager | — (unassign) |
| `Assigned` | `InProgress` | Assignee only | Records `started_at` |
| `InProgress` | `OnHold` | Assignee, Manager | Hold reason from enum: `AwaitingParts`, `AwaitingAccess`, `AwaitingVendor`, `AwaitingShutdownWindow`, `Other` (+ free text) |
| `OnHold` | `InProgress` | Assignee, Manager | — |
| `InProgress` | `Completed` | Assignee only | Work summary, failure cause code, time spent, asset return-to-service flag |
| `Completed` | `Closed` | Manager | For P1/P2: reviewer must differ from the person who completed it |
| `Completed` | `InProgress` | Manager | Rejection reason (rework) |
| `Closed` | `InProgress` | Manager | Reopen reason. Reopen within 30 days counts against first-time-fix rate |
| `Open`, `Assigned`, `OnHold` | `Cancelled` | Manager | Cancellation reason |

Every other transition is invalid and must be rejected by the domain, not by the UI.

Statuses are **not** free text and **not** a user-configurable workflow engine. That is a
deliberate scope cut — a configurable state machine is a different product.

---

## 5. Priority and SLA

| Priority | Definition | Respond within | Resolve within |
|---|---|---|---|
| P1 | Safety risk, or site cannot trade | 1 hour | 4 hours |
| P2 | Major function lost, workaround exists | 4 hours | 24 hours |
| P3 | Minor function or comfort | 1 business day | 5 business days |
| P4 | Cosmetic / improvement | none | backlog |

- The requester proposes urgency; the manager sets the authoritative priority. Both values are
  retained — divergence between them is itself a useful signal.
- SLA is measured against the **site's** timezone and business hours.
- "Respond" = transition to `Assigned`. "Resolve" = transition to `Completed`.

---

## 6. Preventive maintenance

A **PMPlan** attaches to an Asset (or an Asset category across a Site) and generates WorkOrders.

**Trigger types in scope:**

1. **Fixed calendar** — next due date computed from the schedule, independent of completion.
   A PM due monthly on the 1st, completed on the 9th, is still next due on the 1st.
2. **Floating calendar** — next due date computed from actual completion date.
   A 30-day PM completed 5 days late is next due 30 days after that completion.

**Deferred, with reason:** meter-based triggers (runtime hours, cycles). Excluded because they
require a `MeterReading` entity and a threshold evaluator with its own ingestion story. But the
generator must be written **polymorphic over trigger type from day one**, so adding it is a new
strategy, not a rewrite.

**Generation rules — these are the sprint's real content:**

- The generator runs on a horizon: create WorkOrders `N` days before due date.
- **Never two open WorkOrders for the same (plan, asset) pair.** This is the natural uniqueness
  constraint and the reason PM generation must be idempotent rather than "runs nightly and hopes."
- Missed window policy: **fixed** plans skip missed occurrences and generate only the next one;
  **floating** plans shift forward. Skipped occurrences are recorded, not silently dropped —
  PM compliance depends on knowing what was missed.
- Editing a plan does not retroactively alter already-generated open WorkOrders.
- Retiring the asset suspends the plan.
- Due dates resolve in the **site's** timezone. DST transitions must not duplicate or skip a run.

---

## 7. Metrics the product must be able to compute

These define what the dashboard, reporting, and search sprints actually have to produce. If a
query cannot serve one of these, it is decoration.

- **MTTR** — mean time from Request submission to WorkOrder `Completed`, by site / asset /
  category / priority.
- **SLA attainment** — % of work orders meeting respond and resolve targets, by priority.
- **PM compliance** — % of PM work orders completed within their due window.
- **Backlog** — count and age distribution of work orders not yet `Closed`.
- **First-time fix rate** — % of closed work orders not reopened within 30 days.
- **Bad actors** — assets ranked by work order count and total time spent, trailing 12 months.
- **Triage throughput** — median time from Request `Submitted` to triage decision; rejection and
  duplicate rates by site.

---

## 8. Scale envelope

Design targets. These make "assume meaningful load" testable rather than rhetorical.

| Dimension | Target |
|---|---|
| Organizations | 500 |
| Sites — largest tenant | 400 |
| Assets — largest tenant | 25,000 |
| Users — largest tenant | 4,000 (mostly Requesters) |
| Requests | 60,000 / month across platform |
| Work orders | 2,500 / day across platform |
| Attachments | ~2 photos per completed work order, 3 MB average |
| History retention | 7 years → ~6M work orders, ~12M attachments at steady state |
| Read:write ratio | roughly 20:1 |
| Peak shape | weekday mornings, site-local — spread across timezones |

Any query in any sprint must be evaluated against **the largest tenant**, not the average one.

---

## 9. Explicit non-goals

Naming these prevents scope drift and makes deferral a decision rather than an oversight.

| Excluded | What it would have added |
|---|---|
| Parts inventory and purchasing | Stock levels, reservations, POs — a second domain of comparable size |
| Vendor / contractor portal | External identity, quotes, invoices, a second tenancy model |
| Labor cost, rates, payroll | Financial correctness requirements |
| Mobile offline sync | Conflict resolution and client-side state — genuinely hard, unrelated to server craft |
| IoT / condition monitoring | Time-series ingestion at a different order of magnitude |
| Configurable workflow builder | Turns a domain model into a rules engine |
| Multi-language / multi-currency | Broad, shallow, low learning value |

---

## 10. Sprint sequence

Each sprint is a complete user-visible workflow, as the engineering principles require.

**Part 1 — Foundations** (defined in `MaintainOps-Engineering-Principles.md`)

**Part 2 — Product sprints**

1. A user creates an organization and becomes its admin
2. An admin invites and manages members with site-scoped roles
3. An admin manages sites (with timezone, business hours)
4. An admin manages locations and the asset register
5. Site staff report a maintenance problem
6. A manager triages requests — accept, reject, merge duplicates
7. A manager assigns work and technicians see their queue
8. A technician starts, holds, and completes work
9. Users communicate through comments and see an activity timeline
10. A technician attaches photo evidence at completion
11. A manager reviews and closes completed work
12. A manager defines preventive maintenance plans
13. The system generates preventive work orders on schedule
14. Users receive notifications about work affecting them
15. A manager monitors live operations and SLA breach risk
16. Users search and report across maintenance history
17. Auditors review sensitive actions
18. The system is exercised under multi-tenant load at the stated envelope

Sprints 5–8 are the spine. If those are modelled correctly, everything after is extension rather
than repair.

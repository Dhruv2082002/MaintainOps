# Foundations, increment 3: API conventions design, step 1 (error handling)

Session date: 2026-11-20. Status: design agreed, step 1 of 5 built and tests green. Not committed yet.

## What this increment is

The conventions every endpoint inherits: how errors look (ProblemDetails), how requests are
validated, the OpenAPI document, and cancellation. Plus a few global JSON and routing choices that
are cheap now and breaking later.

## Decisions made

| Area | Decision | Why / notes |
|---|---|---|
| Expected failures | Throw our own exception types and map them in one `IExceptionHandler`. It handles only those types and returns `false` for anything else | Less plumbing than a Result type. Handling only known types keeps real crashes logged (see Concepts) |
| Status codes | 400 field validation, 401/403 auth, 404 not found, 409 conflicts with current state, 422 business rule refused, 429 rate limited, 500 unexpected | 400 = fix a field, 409 = reload, something changed, 422 = valid but not allowed |
| Other tenant's data | 404, not 403 | 403 would confirm the resource exists |
| Error body | Every refusal carries a stable `code` (e.g. `workorder.invalid_transition`); `detail` is a plain sentence for the user | The frontend branches on `code`, never on text |
| Validation | DataAnnotations on request DTOs, no FluentValidation | Hard rules live in the domain, so requests only need simple field checks |
| Cross-field checks in the domain | Thrown as a business rule, so 422 | No extra exception type just to get a 400 |
| Validation errors | Same ProblemDetails shape, keys match JSON names (`name`, not `Name`) | So the frontend can put each message under its input |
| OpenAPI | Built-in `Microsoft.AspNetCore.OpenApi`, Development only, no UI package | I browse and call the API in Apidog |
| `openapi.json` | Written into the repo at build, imported into Apidog | PR diffs show contract changes |
| Cancellation | Every async action takes a `CancellationToken` and passes it to all I/O. Client disconnect = 499, Information log, not a 500 | A user closing the tab isn't an error |
| Routes | Everything under `/api` | |
| Versioning | None for now | One client, deployed with the API |
| Enums | Strings in JSON. Responses send `{ "value": "InProgress", "display": "In progress" }`, requests send the plain string. `[Display(Name = ...)]` on every value, and a test fails if one is missing | Raw enum names never reach the screen |
| Pagination | Decided with the first list endpoint (Sprint 3, sites). Expect a common wrapper | No real query to design it around yet |
| Tests | Integration tests on `WebApplicationFactory`, against a controller that exists only in the test project | The API has no real failing endpoint yet |

## Order of work

1. Error handling: exception types, handler, ProblemDetails: **done**
2. Validation: DataAnnotations 400 in the same shape
3. JSON and routes: string enums with display text, `/api` prefix (the deploy smoke test moves to `/api` too)
4. OpenAPI: document in Development, error responses on each endpoint, `openapi.json` at build, Apidog import
5. Cancellation: tokens through, 499 handling

## What step 1 built

| File | What it does |
|---|---|
| `Domain/Exceptions/ConflictException.cs`, `BusinessRuleException.cs` | 409 and 422. In Domain because the domain throws them, and Domain references nothing |
| `Application/Exceptions/NotFoundException.cs` | 404. In Application because lookups happen there |
| `Api/ErrorHandling/KnownExceptionHandler.cs` | Maps the three types to status, title, `detail` and `code`; returns `false` for anything else |
| `Api/ErrorHandling/ProblemDetailsCodes.cs` | The `code` field name and `unexpected_error` |
| `Api/Program.cs` | `AddProblemDetails` (with the 500's title and code), `AddExceptionHandler`, `UseExceptionHandler` first in the pipeline |
| `IntegrationTests/ErrorHandling/*` | Test-only controller, a factory that adds it, and the tests |

Each exception takes a code and a plain message: `throw new ConflictException("workorder.invalid_transition", "This work order is on hold, so it can't be completed.")`.

## Concepts worth remembering

**How a failed request flows.** `UseExceptionHandler` is first in the pipeline, so it wraps
everything after it. When a controller throws, the exception travels back up to it. It asks each
registered `IExceptionHandler` "is this yours?". Ours answers `true` for its three types and writes
the response. If nobody claims it, the middleware logs an error and writes a 500.

**Handled means not logged (.NET 10).** When a handler returns `true`, .NET 10 doesn't log the
exception. A handler that claims every exception would hide real crashes. That's why ours
returns `false` for anything it doesn't know. Checked: the crash test logs a `fail:` line with the
full exception, and the 404/409/422 tests log nothing.

**Middleware order matters.** The exception middleware only catches what runs after it, so it
goes first.

**`IProblemDetailsService` is the one writer for error JSON.** `AddProblemDetails` registers it.
It fills in `type` and `traceId`, and runs the `CustomizeProblemDetails` hook on every error
response. The 500 comes from the built-in middleware, not our handler, so the hook is where it gets
its plain title and `code`.

**Why the 500 doesn't leak.** The default writer never puts the exception into the body. The test
checks that neither the message nor the exception type appears.

**`title` and `detail`.** `title` is a short, general summary for the kind of problem. `detail` is
the specific sentence for this case, written by whoever threw.

**.NET 10's new built-in validation (`AddValidation()`) is for Minimal APIs only.** With
controllers, `[ApiController]` runs DataAnnotations and returns the 400 before the action runs.

**Application parts.** The API only finds controllers in its own project. `AddApplicationPart`
in the test factory makes it find the test-only controller too.

**Primary constructors.** `class ConflictException(string code, string message) : Exception(message)`
puts the constructor on the class line.

## How to run it

- **Tests:** open `backend/` in its own VS Code window → Testing view → MaintainOps.Api.IntegrationTests
  → ErrorHandling → ErrorResponseTests → Run.
- **See a response body:** breakpoint in the theory test after the body is read → right-click the
  test → Debug Test → look at `problem`.
- **See the crash log:** C# Dev Kit's Test Results shows only per-test output, not the API's
  console log. Terminal in `backend/`:

  ```
  dotnet test tests/MaintainOps.Api.IntegrationTests --filter "FullyQualifiedName~Unexpected" --logger "console;verbosity=detailed"
  ```

  Look for the `fail:` line.

## Open items

- A URL that matches no route returns an empty 404, not ProblemDetails (`UseStatusCodePages` would
  fix it). Not decided yet.
- 404/409/422 aren't logged at all. Revisit in increment 4 (observability).
- Apidog project ID still to be added, so the import can run.

## Next

Step 2: validation.

# Foundations, increment 1: repo, solution skeleton, CI

Session date: 2026-11-14. Status: finished and pushed to `dev`, CI green.

## What this increment is

Part 1 (Foundations) of the roadmap comes before the product sprints. It has no user journey of
its own, so the plan is a **walking skeleton** as the definition of done: a signed-in user, a
protected `GET /me`, correlation IDs through logs and traces, ProblemDetails errors shown properly
in the UI, deployed to Azure by GitHub Actions with tests passing in CI. Every foundation item gets
proven by that thin path instead of only being configured.

Planned increments, in order:

1. Repo and solution skeleton, CI (**done**)
2. Bicep, bootstrap identity, ACR, the two App Services, deploy workflow on `main`
3. API conventions: ProblemDetails, validation, OpenAPI, cancellation
4. Observability: structured logging, correlation, tracing, metrics, health checks
5. Frontend shell: routing, API client, error-handling conventions
6. Authentication, authorization infrastructure, session handling, ending with `GET /me`
7. Security baseline: rate limiting, security headers, CORS and CSRF stance, secret handling
8. Test harness rounded out: unit, integration, component, one E2E test

## Decisions made

| Area | Decision | Why / notes |
|---|---|---|
| Frontend | React + TypeScript (Vite) | Chosen by me |
| Identity | ASP.NET Core Identity | I'm comfortable in ASP.NET. It stores users in a database, so **PostgreSQL moved into Foundations** (the Environments section of the principles doc was updated) |
| Deployment shape | Separate App Services for web and API, as Docker images | Both can share one App Service Plan, so it is not double the cost |
| Registry | Azure Container Registry, Basic tier (about $5/month, 10 GiB) | App Service pulls with a managed identity, so no registry passwords. No permanent free tier. Price came from a third-party site, so confirm it on Microsoft's pricing page |
| Architecture | Layered Clean Architecture | Domain, Application, Infrastructure, Api |
| API style | Controllers, not Minimal APIs | |
| Session model | Token held by the frontend, with a refresh-token flow | Custom domain gets attached at the end; that only changes the API's allowed origin (CORS) |
| Branches | `dev` = CI only, nothing deploys. `main` = deploys, with an approval guard | Lets me push work from another PC without publishing to Azure |
| Repo | Public, `Dhruv2082002/MaintainOps` | Public repos get unlimited Actions minutes and free branch protection |
| Rejected | Architecture tests, warnings-as-errors, Dependabot | Not needed for a learning project |
| Linter | oxlint (kept) | See "Things that went differently" |

## Concepts worth remembering

**BFF and cookies versus tokens.** The question is where the browser keeps proof that the user is
logged in. A token in JavaScript is simple but any injected script (XSS) can steal it. A cookie
marked HttpOnly can't be read by scripts. BFF (Backend-for-Frontend) is the name for designs built
around the cookie, where the server handles the tokens. I chose the token route.

**Why the cookie route is awkward with separate deployments.** Two default Azure hostnames
(`...-web.azurewebsites.net` and `...-api.azurewebsites.net`) count as different sites to the
browser, so the API's cookie becomes a cross-site cookie. Safari and Firefox block those by default.
A custom domain (`app.` and `api.` under one domain) fixes it.

**Central build configuration in .NET.**
- `global.json` pins the SDK version so every PC and CI builds the same way.
- `Directory.Build.props` is picked up automatically by every project below it. Target framework,
  nullable and implicit usings are set once there, so the layer `.csproj` files are nearly empty.
- `Directory.Packages.props` (central package management) holds every NuGet version in one place.
  Projects list packages without a version.

**Layer rule.** Dependencies point inward: Api → Infrastructure → Application → Domain, and Domain
references nothing. The project references are the only enforcement now, since I declined
architecture tests.

**`WebApplicationFactory<Program>`.** Boots the real API in memory (same `Program.cs`, middleware
and controllers) and gives an `HttpClient` wired straight to it, without opening a network port.
This is the harness later tests for auth, validation and ProblemDetails will run on. In .NET 10 no
`public partial class Program {}` is needed.

**TypeScript 7 is strict by default.** The Vite template's `tsconfig` doesn't set `strict`, but I
confirmed with a throwaway file that implicit `any` and assigning `null` to `string` both fail.

## What exists now

```
backend/   MaintainOps.slnx, global.json, Directory.Build.props, Directory.Packages.props
  src/     Domain, Application, Infrastructure (empty), Api (RootController: GET / -> {"name":"MaintainOps"})
  tests/   Domain.Tests, Application.Tests (empty template tests), Api.IntegrationTests (tests GET /)
frontend/  Vite 8 + React 19 + TypeScript 7, oxlint, Prettier, Vitest 5, blank "MaintainOps" page
docs/      Product definition and engineering principles
.github/workflows/ci.yml   Backend and Frontend jobs, every push and PR, never deploys
```

Commit `6aa4e29` on `dev`. CI run: Backend success, Frontend success.

## Things that went differently than planned

- **`Microsoft.AspNetCore.Mvc.Testing` 11.0.0** is built for .NET 11 and refused to install on
  net10.0. .NET 11 has been released since we chose 10. I pinned **10.0.15** and stayed on .NET 10
  (the long-term support release).
- **The Vite template ships oxlint, not ESLint.** The plan said ESLint; I chose to keep
  oxlint.
- **`dev` is currently the repo's default branch**, because it was pushed first.

## Open items

- `frontend/README.md` is still the Vite boilerplate, and the npm package is still named `frontend`.
- NuGet packages aren't cached in CI (needs lock files, not discussed). npm is cached.
- Switch the repo's default branch to `main` when it exists (increment 2).
- The Sprint 1 product questions are parked until Foundations is done: can a user belong to more than
  one org, can one user create several orgs, is email verification required, and is editing the
  org name part of Sprint 1.

## Next

Increment 2: Bicep, the one-time bootstrap (resource group and the identity GitHub Actions deploys
with), ACR, the App Service Plan and two App Services, and a deploy workflow that runs only on
`main`. Starts with its own design discussion first, including App Service Plan options and prices.

# MaintainOps

A maintenance management system for organizations that operate many similar physical locations
(retail chains, restaurant groups, gyms, clinics). Built as a learning project for engineering
production-grade applications.

- What is built: [docs/MaintainOps-Product-Definition-v2.md](docs/MaintainOps-Product-Definition-v2.md)
- How it is built: [docs/MaintainOps-Engineering-Principles.md](docs/MaintainOps-Engineering-Principles.md)

## Repository layout

```
backend/                      ASP.NET Core API, layered Clean Architecture
  src/
    MaintainOps.Domain/           entities and business rules; references nothing
    MaintainOps.Application/      use cases; references Domain
    MaintainOps.Infrastructure/   persistence, identity, external services; references Application
    MaintainOps.Api/              HTTP endpoints (controllers), composition root
  tests/
    MaintainOps.Domain.Tests/
    MaintainOps.Application.Tests/
    MaintainOps.Api.IntegrationTests/
frontend/                     React + TypeScript (Vite)
docs/                         product definition and engineering principles
learning-notes/               session notes: decisions, concepts, what went differently
.github/workflows/            CI
```

## Prerequisites

- .NET SDK 10.0.400 (pinned in `backend/global.json`)
- Node.js 24.19.0 (pinned in `frontend/.nvmrc`)

## Running locally

Backend:

```
cd backend
dotnet build MaintainOps.slnx
dotnet test MaintainOps.slnx
dotnet run --project src/MaintainOps.Api
```

Frontend:

```
cd frontend
npm install
npm run dev
```

Other frontend scripts: `npm run lint`, `npm test`, `npm run build`, `npm run format`.

## Branches

- `dev`: day-to-day work. CI builds and tests every push; nothing is deployed.
- `main`: merging `dev` into `main` is how a release ships. Only `main` deploys (the deploy
  workflow is not set up yet).

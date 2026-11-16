# Foundations, increment 2: design for Azure infrastructure and deploy

Session date: 2026-11-16. Status: design agreed, nothing built yet.

## What this increment is

Get the near-empty app running on Azure, created by Bicep and deployed by GitHub Actions. Deploying
early is standard practice ("walking skeleton"): deployment problems show up while the app is tiny,
and cloud pieces (database, secrets, monitoring) are added later by the increments that need them.

## Decisions made

| Area | Decision | Why / notes |
|---|---|---|
| Hosting | One B1 Linux App Service Plan with two App Services: API image and frontend image | B1 is the cheapest tier that runs Docker images. The Free tier most likely can't |
| Frontend | Docker image: Node builds the app, unprivileged nginx serves it, API URL set at build time | Chose Docker over the free Static Web Apps, to practise containers |
| Registry | ACR Basic | Standard Azure setup, kept from increment 1 |
| Region | Central India | Closest |
| Deploy identity | User-assigned managed identity. Trusted only for GitHub jobs in the `production` and `teardown` environments. Contributor plus Role Based Access Control Administrator (limited to granting AcrPull) on the resource group only | No password anywhere. Can't reach outside the group or give itself more rights |
| Bootstrap | Done by hand in the Azure portal, not a script | I learn more doing it myself |
| Infrastructure | `infra/main.bicep` at resource-group scope, modules for registry and App Service | PostgreSQL, Key Vault and Application Insights arrive with the increments that need them |
| Image tags | Git commit SHA, never `latest` | Always know which commit is live. Rollback = revert and redeploy |
| Approval guard | GitHub `production` environment with me as required reviewer. `main` protected: PR from `dev`, CI green | |
| Deploy | `deploy.yml`, on push to `main` or by button: sign in, `what-if`, Bicep, build and push images, restart, smoke test `GET /` | |
| Teardown | `teardown.yml`, by button and nightly at 3 AM IST (`30 21 * * *` UTC). Deletes the plan (and both apps) and ACR | Runs in its own `teardown` environment with no reviewer, so the nightly run doesn't wait for approval |
| Naming | Microsoft abbreviations plus a suffix made from the resource group (`rg-maintainops-prod`, `app-maintainops-api-<suffix>`, `acrmaintainops<suffix>`) | Same names every time it is recreated, and globally unique |
| Default branch | Create `main` after the first deploy works, make it default | |

Also: the principles doc now says PostgreSQL and EF Core are the exception to "requirement-driven",
since ASP.NET Core Identity needs them in Foundations.

## Concepts worth remembering

**Pay-as-you-go means paying for existence, not traffic.** App Service, ACR and PostgreSQL hold
capacity for you and bill for every hour (ACR: every day) they exist, used or not. Prices are listed
per month but billed per hour. Stopping an App Service doesn't stop billing; the plan has to be
deleted. A PostgreSQL Flexible Server can only stay stopped 7 days before Azure restarts it, and its
storage is billed while stopped.

**So: delete between sessions.** Teardown deletes everything that costs more than cents. Kept: the
resource group, deploy identity, its trust and permissions (all free), and later Key Vault and
Application Insights (effectively $0 at this usage). Idle cost about $0, a 2-hour session about
$0.25. Leaving everything running would be about $18/month, about $30–35 once PostgreSQL arrives.
These prices are from memory; confirm in Azure's pricing calculator.

**Bicep describes what should exist.** Running it creates whatever is missing and leaves alone
what already matches. That's why a push to `main` works even after teardown: the deploy recreates
the resources first, then puts the app on them.

**Bootstrap is the chicken-and-egg step.** GitHub Actions needs an Azure identity with permission
before it can create anything, and it can't create that identity itself. So, once, by hand: resource
group, deploy identity, trust link to the GitHub environments, permissions on the group, and the
three IDs (client, tenant, subscription) into GitHub variables. They aren't secrets.

**From `dotnet publish` to a running app on Azure:**
- **Image:** the publish output plus the .NET runtime and a small Linux system, in one file.
- **ACR:** private storage for images.
- **App Service:** runs an image from ACR.
- **Managed identity:** an account Azure creates for the App Service, with no password.
- **AcrPull:** the permission that lets that account download images from the registry.
- Bicep creates the identity and the permission every time it recreates the App Service. That's
  why the deploy identity needs permission to grant permissions.

**Secrets after teardown.** Nothing to re-enter: the GitHub sign-in uses the trust link, the
managed identities and permissions are in Bicep, and app secrets will live in a Key Vault that
teardown keeps.

## Order of work

1. Dockerfile for the API, run locally
2. Dockerfile for the frontend, run locally
3. Bootstrap in the portal
4. Bicep
5. Deploy workflow
6. Teardown workflow
7. Create `main`, protect it, first real deploy

**Done when:** a merge to `main` gets approved, the deploy creates everything from nothing, both
sites answer on their Azure URLs, and teardown takes it back to $0.



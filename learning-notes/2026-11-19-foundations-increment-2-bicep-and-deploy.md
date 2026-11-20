# Foundations, increment 2: Bicep, deploy and teardown workflows, first deploy

Session dates: 2026-11-18 and 2026-11-19. Status: **increment 2 done**. Next is increment 3 (API
conventions).

## Progress on the order of work

1. Dockerfile for the API, run locally: done (earlier)
2. Dockerfile for the frontend, run locally: done (earlier)
3. Bootstrap in the portal: done (earlier)
4. Bicep: **done**, commits `4c252d6` and `3c77291`, outputs added in `ff6fb15`. What-If: 6 Creates
5. Deploy workflow: **done**, commit `ff6fb15`
6. Teardown workflow: **done**, commit `cbf4bce`
7. Create `main`, protect it, first real deploy: **done**

**Done when** (from the design note), all met: the merge to `main` got approved, the deploy created
everything from nothing, both sites answered on their Azure URLs, and teardown took it back to only
the deploy identity.

## How the Bicep part went

A quiz of 7 variants of all three App Service files, before I had written any of them, was
overwhelming: it tested memorised details without the shape they hang on. What worked instead:
the shape first (plan, site, permission, wiring), then one file at a time with the reason for every
line, then the files themselves once I understood them.

## Decisions made

| Area | Decision | Why / notes |
|---|---|---|
| Bicep modules | `registry.bicep`, `appServicePlan.bicep`, `appService.bicep` (used twice: API and web) | One definition of "a site", called with different values |
| Bicep outputs | Registry name and login server, site names, API and web URLs | The workflow can't compute names (the suffix comes from `uniqueString`), so Bicep reports them |
| Deploy steps | Plain `az` and `docker` commands, not dedicated actions | Same commands work on my PC; nothing extra to learn |
| What-If in deploy | Kept as a log entry, not a gate | Approval happens before the job starts. A real gate would need two jobs; not chosen |
| Teardown | Deletes by type, in order: sites, plans, registries | Leaves the deploy identity (and later Key Vault, App Insights) alone. PostgreSQL's type gets added when it arrives |
| Concurrency | Deploy and teardown share the group `deploy`, never cancelled midway | They can't overlap; a half-finished deploy never gets cut off |
| `main` ruleset | PR required, 0 approvals, merge commits only, `Backend` and `Frontend` must pass, no deletion or force push, no bypass | GitHub won't let me approve my own PR, so the approval lives on the deploy. Squash/rebase would make `dev` and `main` drift |
| `production` environment | Not restricted to `main` | Not worth it: deploys only start on `main` by themselves, and I approve every run |
| Default branch | `main` | Scheduled workflows and Run workflow buttons use the default branch's copy |

## Concepts worth remembering

**Plan and site, in IIS terms.** The App Service Plan is the server: its size (B1) sets the hourly
price, and it bills while it exists. A site is one IIS site on it and costs nothing extra. That's
why teardown deletes the plan, and why two sites on one plan isn't double the cost.

**`reserved: true` is what makes a plan Linux.** `kind: 'linux'` is mostly a label.

**Outputs set the order.** Passing one module's output into another tells Bicep to create the first
one first. Nothing says "do this first" explicitly. Passing a plain string instead would compile but
lose the ordering.

**The site's identity is system-assigned:** created and deleted with the site, so a new one after
every teardown. That's why the AcrPull grant is in Bicep, not done once by hand.

**A role assignment is who, what, where.** Who: the site's identity. What: AcrPull, as the full
role ID (`subscriptionResourceId(...)`). Where: the registry only. Its name is a `guid(...)` of the
registry, site and role, so a rerun finds the same assignment instead of adding a duplicate.
`site.id` keeps the API's and web's names different. `principalType: 'ServicePrincipal'` matches the
deploy identity's condition. Deleting the registry removes its assignments (checked after teardown).

**What the site runs.** `linuxFxVersion: 'DOCKER|<registry>/<image>:<tag>'`.
`acrUseManagedIdentityCreds: true` pulls with the site's own identity (no registry password); it
must be inside `siteConfig`. `WEBSITES_PORT` is like an IIS binding: 5286 for the API, 8080 for nginx.

**Password-free sign-in from GitHub.** `permissions: id-token: write` lets the job get a signed
token. `environment: production` puts `environment:production` into its subject, which must match
the trust link exactly. `azure/login` swaps it for an Azure sign-in as the deploy identity.

**Passing values between steps.** A step appends `name=value` lines to `$GITHUB_OUTPUT`; later steps
read `${{ steps.<id>.outputs.<name> }}`. `jq` turns Bicep's outputs into those lines.

**`shell: bash` turns on `pipefail`.** Without it a pipe's step passes if its last command passes,
so a failed deploy piped into `jq` would show green.

**Why every deploy restarts the sites.** Bicep points the sites at the new commit's tag before that
image is pushed, so they can only pick it up after the push. Side effect: a few minutes of possible
downtime per deploy. Acceptable for now.

**Smoke test.** A green Bicep step only means Azure accepted the config. `curl --fail` with retries
proves the container actually started. On Deploy #1 the first attempt timed out (site still
starting) and the second returned `{"name":"MaintainOps"}`.

**Reading the Actions page.** A push-triggered run is titled with the commit's message, so one
commit can show up as CI on `dev`, CI on `main` and Deploy with the same title. The grey line under
it says which workflow and run number. The grey line at the top of each step's log shows the
command with every `${{ }}` filled in.

**Schedules.** Cron is UTC (`30 21 * * *` = 3:00 AM IST) and runs only from the default branch.

## How to use it

- **Bring up:** Actions → Deploy → Run workflow (`main`) → open the run → Review deployments →
  production → Approve and deploy. About 10 minutes.
- **Take down:** Actions → Teardown → Run workflow. No approval. Also runs nightly at 3 AM IST.
- **Find the URLs:** Azure portal → resource group → the site → Overview → Default domain. Or VS Code
  Azure view → App Services → right-click → Browse Website.
- **Day to day:** work on `dev` → PR to `main` → CI green → merge → approve the deploy.

## What exists now

```
infra/main.bicep                     registry, plan, api and web modules; imageTag param; outputs
infra/modules/registry.bicep         ACR Basic, outputs loginServer and name
infra/modules/appServicePlan.bicep   B1 Linux plan, outputs id
infra/modules/appService.bicep       container site + its AcrPull grant, outputs hostName and name
.github/workflows/deploy.yml         push to main or button; production environment
.github/workflows/teardown.yml       nightly 3 AM IST or button; teardown environment
```

GitHub: `main` exists, is the default branch and is protected. Azure right now: torn down, only
`id-maintainops-deploy` in `rg-maintainops-prod`.

## Things that went differently

- **Creating `main` started Deploy #1 by itself** (it counted as a push to `main`). It waited for my
  approval, as intended.

## Open items

- The nightly 3 AM teardown hasn't been seen running on schedule yet (the manual run worked).
- The smoke test only checks the API, not the web site.
- No Docker build caching in the deploy; every run builds both images from scratch.
- The frontend has no API URL build setting yet; it arrives with the frontend shell (increment 5).
- Sprint 1 product questions are still parked until Foundations is done.

## Next

Increment 3: API conventions (ProblemDetails, validation, OpenAPI, cancellation), starting with its
design discussion.

# Foundations, increment 2: Docker images and Azure bootstrap

Session date: 2026-11-17. Status: steps 1–3 of increment 2 done. Next is step 4 (Bicep).

## Progress on the order of work

1. Dockerfile for the API, run locally: **done**, commit `a25820d`
2. Dockerfile for the frontend, run locally: **done**: `frontend/Dockerfile`, `nginx.conf` and
   `.dockerignore` (Node builds, unprivileged nginx serves on port 8080). Not committed yet
3. Bootstrap: **done**, by hand in the portal, then corrected with the Azure CLI (see below)
4. Bicep: next
5. Deploy workflow
6. Teardown workflow
7. Create `main`, protect it, first real deploy

## What the bootstrap created

**Azure** (subscription `Pay-As-You-Go`):

| Thing | Value |
|---|---|
| Resource providers registered | `Microsoft.Web`, `Microsoft.ContainerRegistry` (and `Microsoft.ManagedIdentity`) |
| Resource group | `rg-maintainops-prod`, Central India |
| Deploy identity | `id-maintainops-deploy`, user-assigned managed identity, Central India |
| Trust links (federated credentials) | `github-production` and `github-teardown` |
| Permissions on the group | Contributor, and Role Based Access Control Administrator limited to granting AcrPull to service principals |

**GitHub** (`Dhruv2082002/MaintainOps`):

| Thing | Value |
|---|---|
| `production` environment | Me as required reviewer, *Prevent self-review* off |
| `teardown` environment | No rules, so the nightly run doesn't wait |
| Repository variables | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (IDs, not secrets) |

## Concepts worth remembering

**Resource providers.** Each Azure service (App Service = `Microsoft.Web`, ACR =
`Microsoft.ContainerRegistry`) has to be switched on for the subscription before anything of that
kind can be created. Switching one on is a subscription-level action. The deploy identity only has
rights inside the resource group, so it couldn't do it itself. That's why it's part of bootstrap.

**The trust link must match exactly.** Azure compares the federated credential's *subject* with the
`sub` claim in GitHub's token, character for character. A wrong subject saves without any error. It
only shows up later as a failed sign-in (`AADSTS70021` / `AADSTS700213`).

**GitHub's new subject format.** This repo sends the format with immutable IDs:

```
repo:Dhruv2082002@134192683/MaintainOps@1464528353:environment:production
```

`134192683` is my GitHub user ID (a personal account counts as the "organization"), `1464528353` is
the repo ID. The IDs never change, even if the repo or account is renamed, which is why Azure's
portal now asks for them. The older `repo:owner/repo:...` format would not match this repo.

**Limiting what the deploy identity can grant.** Bicep has to give each new App Service the AcrPull
permission, so the deploy identity needs to grant permissions. The condition on its RBAC
Administrator role allows granting and removing only AcrPull, and only to service principals. An
App Service's managed identity counts as a service principal.

**A managed identity's region can't be changed.** The region only says where its record is stored;
it works for resources in any region. Moving it means deleting and recreating it, which gives a new
Client ID. Then the trust links, permissions and the `AZURE_CLIENT_ID` variable all have to be
redone.

## Things that went differently

- **Two mistakes in the portal work.** The identity was created in East US (the Region box wasn't
  changed), and the `production` trust link had a trailing space in its subject. Both were fixed by
  deleting the identity and recreating everything for it with the Azure CLI, then updating
  `AZURE_CLIENT_ID` in GitHub.
- **The portal's GitHub form had changed** since the docs the guide was written from: it now asks
  for Organization ID and Repository ID.
- **Azure CLI installed** (2.93.0, via winget) and signed in, so Azure state can be checked from the
  terminal instead of from portal screenshots.

## Open items

- Commit the three frontend Docker files.
- The sign-in from GitHub to Azure is untested until the first deploy workflow run (step 5).

## Next

Step 4: Bicep. `infra/main.bicep` at resource-group scope, with modules for the registry and App
Service (one B1 Linux plan, two App Services), names built from the resource group as agreed in the
design note.

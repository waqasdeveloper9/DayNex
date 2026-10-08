# DayNex customer login

DayNex uses Microsoft Entra External ID in an **external tenant**. Customers sign in with Google through Microsoft's hosted browser flow. Microsoft issues the access token. DayNex never receives a Google password and does not issue its own login tokens.

The gateway and each API validate the signature, issuer, API audience, expiration, tenant, token version, and stable customer identity. Protected requests also need the delegated `access_as_user` scope. ID tokens, Microsoft Graph access tokens, and app-only tokens are not accepted as customer API tokens.

Roles and subscriptions belong to the Identity database. The current permission is `holidays.sync`, granted only to database `Admin` and `SuperAdmin` users. A premium subscription does not make someone an administrator. New accounts always start as `User` with a `Free` subscription.

## Microsoft and Google configuration

1. Create or select an Entra External ID **external tenant for customers**. A workforce tenant's employee/guest configuration is not interchangeable with this setup.
2. Follow [Microsoft's Google federation guide](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-google-federation-customers) to create the Google OAuth credentials and add Google as an identity provider. The Google secret goes in Microsoft configuration, never in this repository.
3. Create a sign-up/sign-in user flow, enable Google, and associate the test client application with it.
4. Register one **DayNex API** application in that tenant. Configure v2 access tokens (`api.requestedAccessTokenVersion = 2`), expose `api://<API_CLIENT_ID>/access_as_user`, and grant the test client delegated access with administrator consent. All current services share this API resource/audience; their database permissions decide which operations are allowed.
5. Register a separate **DayNex Postman test client**. For a predictable Postman setup, use a Web redirect URI matching Postman's browser callback exactly and a short-lived client secret stored only in Postman Vault. This test client can use Authorization Code with PKCE plus its confidential-client secret. Future Android and browser clients use their own registrations and PKCE; never embed the Postman secret in them.
6. In Postman, enable **Authorize using browser** and copy the callback URL shown there into the test client's Web redirect configuration. Do not register it as a SPA redirect for a desktop token exchange.

Reference: [Microsoft API registration](https://learn.microsoft.com/en-us/entra/identity-platform/web-api-tutorial-01-register-app), [authorization code flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow), and [Postman OAuth setup](https://learning.postman.com/docs/use/send-requests/authorization/oauth-20/).

## Backend configuration

Install a supported .NET 10 SDK and runtime. Each executable has a UserSecretsId. Set these values separately for Gateway, Identity API, and Holiday API using development user secrets or environment variables:

| Setting | Value |
| --- | --- |
| `Entra:TenantSubdomain` | External tenant subdomain, without `.ciamlogin.com` |
| `Entra:TenantId` | External tenant directory GUID |
| `Entra:Audience` | DayNex API application's client GUID, **not** the Postman client's GUID |
| `Entra:RequiredScope` | `access_as_user` |
| `ConnectionStrings:DefaultConnection` | Service's own Azure SQL connection string; Identity/Holiday only |
| `IdentityService:BaseUrl` | Identity API's HTTPS address; Holiday only |

Environment variables use double underscores, for example `Entra__TenantId` and `ConnectionStrings__DefaultConnection`. Checked-in placeholders intentionally fail startup until configured. SQL credentials previously committed must be rotated; removing them from current files does not remove Git history.

For example, set a public identifier with:

```powershell
dotnet user-secrets set "Entra:TenantId" "YOUR_EXTERNAL_TENANT_GUID" --project src/Services/DayNex.IdentityService/DayNex.IdentityService.WebApi
```

Set SQL credentials through your local secret store without committing them. Azure SQL connections should use encryption with certificate validation (`Encrypt=True;TrustServerCertificate=False`). Managed identity is also possible when your deployment supports it; it is independent of customer Google login.

Local HTTPS addresses:

| Host | Address |
| --- | --- |
| Gateway | `https://localhost:7250` |
| Identity API | `https://localhost:7166` |
| Holiday API | `https://localhost:7175` |

Trust your development HTTPS certificate with `dotnet dev-certs https --trust`. Start Identity and Holiday using their `https` launch profiles, then start Gateway. Do not disable downstream TLS verification. In Azure, replace the Ocelot downstream hosts, ports, and schemes and `IdentityService:BaseUrl` with actual HTTPS service addresses. Environment overrides such as `Routes__0__DownstreamHostAndPorts__0__Host` take precedence over `ocelot.json`.

## Database migrations

Migration generation uses design-time factories and never opens a database connection. EF database commands read each API's User Secrets, overridden by environment variables, so they use the same configured database as that service. With no configured connection, model generation still works but database commands fail instead of choosing an unexpected database. Runtime does not automatically migrate databases. Generate reviewed SQL scripts with:

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project src/Services/DayNex.IdentityService/DayNex.IdentityService.Infrastructure --startup-project src/Services/DayNex.IdentityService/DayNex.IdentityService.WebApi --output identity-migrations.sql
dotnet ef migrations script --idempotent --project src/Services/DayNex.HolidayService/DayNex.HolidayService.Infrastructure --startup-project src/Services/DayNex.HolidayService/DayNex.HolidayService.API --output holiday-migrations.sql
```

Review and apply each script to its own database before running the new backend. The Identity migration makes email optional, removes email uniqueness, and adds a subscription foreign key. The Holiday migration adds a unique holiday key. Existing orphan subscriptions or duplicate holidays must be reviewed before applying these constraints; the migrations do not silently delete data. See `MigrationPreflight.sql` for read-only checks.

Alternatively, after reviewing the migrations, apply them using Visual Studio's Package Manager Console. These commands read the corresponding API's configured User Secrets without putting a password on the command line:

```powershell
Update-Database -Project DayNex.IdentityService.Infrastructure -StartupProject DayNex.IdentityService.WebApi -Context IdentityDbContext
Update-Database -Project DayNex.HolidayService.Infrastructure -StartupProject DayNex.HolidayService.API -Context HolidayDbContext
```

User `ExternalId` now contains `<tenant-guid>:<object-guid>`. Legacy records created through query parameters are not trusted and are not automatically linked by email. If existing accounts must be retained, explicitly reconcile each account with its verified Entra `tid` and `oid` before changing its ExternalId. Otherwise a verified login creates a separate free user. Rolling back to the old email uniqueness rule requires reconciling duplicate/null email values first.

## Test Google login in Postman

Import `postman/DayNex.postman_collection.json` and `postman/DayNex.local.postman_environment.json`. Select the environment and fill in the tenant identifiers and Postman client ID.

Use Postman's OAuth 2.0 helper with:

| Field | Value |
| --- | --- |
| Grant type | Authorization Code with PKCE |
| Authorize using browser | Enabled |
| Authorization URL | `https://<subdomain>.ciamlogin.com/<tenant-guid>/oauth2/v2.0/authorize` |
| Access token URL | `https://<subdomain>.ciamlogin.com/<tenant-guid>/oauth2/v2.0/token` |
| Client ID | Postman test client GUID |
| Client secret | Test client's secret from Postman Vault |
| Scope | `openid profile offline_access api://<API_CLIENT_ID>/access_as_user` |
| Code challenge method | SHA-256 |
| Client authentication | Send client credentials in body |

Click **Get New Access Token**, choose Google, and complete sign-in in the system browser. Copy the returned **access token** into the environment's local `access_token` value, then leave the collection's Bearer authentication in place. Do not use the ID token. Do not export or commit populated token/secret values.

Run the requests in order. First profile access provisions the database account; repeat access must return the same user ID. Both gateway and direct APIs must return 401 without a valid token. A normal signed-in user must get 403 when attempting holiday sync. For an administrator test, explicitly grant an existing verified test account the database Admin role using an approved administrative process, then test sync with that user's token. There is intentionally no public role-changing endpoint and no automatic first-user administrator.

If Google sign-in succeeds but the API returns 401, check audience, tenant, v2 token configuration, and `oid`/`tid` claims. A 403 usually indicates a missing API scope or database permission. A 404 from `/api/identity/me/permissions` means that user has not yet been provisioned. A 503 during sync means IdentityService could not supply permissions; sync remains blocked.

## Automated verification and performance

```powershell
dotnet restore DayNex.sln
dotnet test DayNex.sln --no-restore -m:1
```

The HTTP tests use test-only RSA-signed tokens and mocked stores; production never enables those signing keys or bypasses Microsoft validation. SQLite tests cover relational constraints and transaction rollback. Optional SQL Server tests apply the actual migrations and exercise concurrent provisioning and sync against newly created, disposable `DayNexTests_<guid>` databases. To run them, set `DAYNEX_TEST_SQLSERVER` to a local test server connection with database creation privileges. Never point this setting at production. Tests drop only databases they created themselves.

Read paths use indexed identity lookup, one database query, and no tracking. DbContexts and HTTP connections are pooled. Provisioning relies on database uniqueness across replicas, not a per-process lock. Permission grants are fresh per request and fetched at most once within a request, so revocation has no multi-minute cache delay. Holiday sync fetches the provider first, then uses a short serializable database transaction over the small holiday catalogue with transient SQL retries and a unique key.

Third-party HTTP calls have bounded timeouts, jittered retries for safe methods, and circuit breaking. POST/PUT/DELETE are not automatically retried. Customer tokens are forwarded only to IdentityService, never to GOV.UK. HTTP redirects are disabled on clients carrying credentials.

Current limits are 120 requests per minute per identity/IP and two syncs per minute per identity, per running instance. These limits are local protection; a multi-instance deployment needs coordinated ingress limits. `/health` is a public process liveness endpoint, not a database readiness guarantee. Azure reverse-proxy deployments must configure `Hosting:KnownProxies` (environment array: `Hosting__KnownProxies__0`, etc.) with their actual trusted ingress IPs so HTTPS and client IP forwarding work. Loopback is trusted by framework defaults; other forwarding headers are ignored unless the proxy is explicitly trusted. Do not enable blanket trust of arbitrary forwarding headers.

This design can scale with additional instances and suitable database capacity. Free hosting/SQL quotas and cold starts determine actual throughput. Automated correctness tests do not establish support for thousands of simultaneous users; benchmark the deployed environment before making that promise.

Entra External ID's core offering has a free MAU allowance; optional identity features, hosting, and SQL have independent pricing/quotas. See [Microsoft's External ID FAQ](https://learn.microsoft.com/en-us/entra/external-id/customers/faq-customers).

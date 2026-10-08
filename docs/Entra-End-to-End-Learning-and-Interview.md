# DayNex: Entra identity, Azure configuration, backend implementation, and interviews

Prepared 7 October 2026. This document describes the checked-in working-tree implementation and the configuration completed during our session. It contains no passwords, client secrets, authorization codes, or tokens.

## 1. What we have achieved—and what is still to verify

You reported successfully obtaining a real Entra token after Google sign-in. That establishes that the browser sign-in and Postman token exchange worked. It does **not yet establish** that your deployed application or local database provisioning works with that token.

| Area | Current evidence |
|---|---|
| Google → Entra → Postman token exchange | Successful, reported by you |
| Customer tenant and client/API registrations | Configured during guided portal setup |
| Database migrations | You reported Update-Database succeeded |
| Backend authentication and permission code | Implemented; inspected for this document |
| Automated tests | Earlier full Release run: 41 passed; later design-time retry changes were build-verified |
| Real token → gateway → profile/database | Next live verification; no successful response supplied yet |
| Administrator holiday sync using real token | Not yet demonstrated |
| Azure hosting, production ingress, and frontend | Not delivered in this session |
| Thousands of simultaneous users | Not benchmarked |

An honest end-to-end delivery statement distinguishes implemented, tested, configured, and deployed work. A generated token is one milestone, not the entire acceptance test.

## 2. Vocabulary: internal and external Entra

“Internal Entra ID” is an informal description. The precise term here is a **workforce tenant**. Your Default Directory is the directory you used for Azure administration. **DAYNEX USERS** is your separate **external customer tenant**.

| Concept | Purpose in DayNex |
|---|---|
| Workforce tenant / Default Directory | Administrator and workload identities associated with managing Azure resources |
| External customer tenant / DAYNEX USERS | Consumer sign-up and sign-in, Google federation, customer app registrations and user flow |
| B2B collaboration in a workforce tenant | Partners invited to collaborate; a different scenario from our public customer app |
| Azure subscription | Billing and resource-management boundary |
| Resource group | Container for related Azure resources, such as hosting and databases |

External ID includes customer and collaboration scenarios. The word “external” alone does not prove an account belongs to an external customer tenant. [Microsoft tenant configurations](https://learn.microsoft.com/en-us/entra/external-id/tenant-configurations).

Your SQL resources can remain associated with Default Directory while customers authenticate through DAYNEX USERS. Customer tokens do not automatically grant Azure subscription access. An external tenant can be linked to a workforce-owned subscription for billing; this does not move customer app registrations into the workforce directory. [External ID billing and subscription relationship](https://learn.microsoft.com/en-us/entra/external-id/external-identities-pricing).

## 3. Four different permission systems

| System | Example | Controls |
|---|---|---|
| Azure RBAC | Contributor on a resource group | Management of Azure resources |
| Entra directory roles | Application Administrator | Management of identities and app registrations |
| OAuth delegated scope | access_as_user | A client calling the DayNex API for a signed-in user |
| DayNex database permissions | holidays.sync from Admin role | A business operation within DayNex |

Giving yourself Entra administrator privileges does not make your DayNex profile an Admin. Likewise, buying Premium does not grant Azure access or holiday administration. Azure RBAC and Entra directory roles govern different resources. [Microsoft role comparison](https://learn.microsoft.com/en-us/azure/role-based-access-control/rbac-and-directory-admin-roles).

## 4. Our identity architecture

```mermaid
sequenceDiagram
    participant U as Customer / browser
    participant P as Postman test client
    participant E as Entra customer tenant
    participant G as Google
    participant W as Gateway
    participant I as Identity API
    participant D as Identity SQL database
    P->>E: Authorize request + API scope + PKCE challenge
    E->>G: Federated sign-in
    U->>G: Authenticate with Google
    G->>E: Federated authentication result
    E->>P: Redirect with authorization code
    P->>E: Code + PKCE verifier + test-client authentication
    E->>P: Access token; other tokens depend on response
    P->>W: GET /api/identity/me + Bearer access token
    W->>W: Validate token and delegated scope
    W->>I: Forward request and bearer token
    I->>I: Validate token and scope independently
    I->>D: Find user by tid:oid; provision if absent
    D->>I: User and subscription
    I->>P: Profile through gateway
```

Google verifies the user's Google identity. Entra brokers that sign-in and issues the token for DayNex. The backend trusts the configured Entra issuer, not an arbitrary Google token. DayNex never receives the Google password. This is federation, not importing a Gmail contact list. [Google federation for customer tenants](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-google-federation-customers).

## 5. Every cloud object we configured

### 5.1 DAYNEX USERS external tenant

Portal: Entra admin center → Settings → Directories + subscriptions → switch to DAYNEX USERS → Entra ID → Overview.

The tenant ID identifies the directory. The primary domain identifies its domain. `daynexusers.onmicrosoft.com` yields the subdomain `daynexusers`. The subdomain is neither a callback URL nor an application ID.

### 5.2 DayNex API registration

Portal: customer tenant → App registrations → DayNex API.

This describes the protected resource accepted by our backend. We configured a single-tenant registration, an Application ID URI, a delegated scope, and v2 access tokens.

| Setting | Purpose |
|---|---|
| Application (client) ID | API identifier used as the backend's Audience GUID |
| Application ID URI, usually api://API_GUID | Prefix used when requesting this API's scope |
| access_as_user | Our exposed delegated permission |
| api.requestedAccessTokenVersion = 2 | Resource requests v2 access-token format |
| Redirect URI | Not required for this resource-only API |

The full requested scope combines URI and scope name: `api://API_GUID/access_as_user`. The backend checks the short name in `scp`. An API registration does not create hosting or a database. [Expose an API](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-configure-app-expose-web-apis).

### 5.3 DayNex Postman registration

Portal: customer tenant → App registrations → DayNex Postman.

This is a separate OAuth client. Its client ID goes into Postman's Client ID field, not the backend Audience. We configured a Web callback, a short-lived client secret, delegated DayNex API permission, and tenant administrator consent. Both registrations should have an assigned owner for discoverability in My APIs.

The secret Value authenticates this test client at the token endpoint. Secret ID is only the credential record's identifier. Future Android and SPA clients need their own platform-appropriate registrations; the Postman secret must not be copied into their shipped code.

Our working callback was `https://oauth.pstmn.io/v1/callback`. It must match the actual Postman request exactly. A different callback is valid only when registered and consistently used.

### 5.4 Google Cloud project and OAuth client

Google Cloud → Google Auth Platform contains branding, audience, test users, and clients. The Google OAuth client is a Web application whose redirect URIs return to Entra federation endpoints. Its client ID and secret go into Entra's Google provider configuration.

This Google client is different from both Entra registrations. It does not issue the DayNex access token. Keep the Google secret outside Git; save it at creation because later views mask it. [Google client management](https://support.google.com/cloud/answer/15549257).

Google test users and testing status allow a controlled learning exercise. Publishing the eventual public app and meeting any applicable verification requirements remain release work. [Google consent configuration](https://developers.google.com/workspace/guides/configure-oauth-consent).

### 5.5 Entra Google provider and user flow

Entra → External Identities → All identity providers → Google stores the Google client credentials. The sign-up/sign-in user flow enables Google and any chosen email method. Its Applications list must include DayNex Postman.

The user flow defines the customer's sign-in experience. The app association selects that experience for the client. Registering the client without attaching it is incomplete. An application can have only one associated user flow. [Attach a client to a user flow](https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-user-flow-add-application).

### 5.6 Why recreating registrations was necessary

We initially found the app registrations in Default Directory. We recreated them in DAYNEX USERS and used their new IDs. App registration objects cannot be moved between tenants. Old IDs and secrets do not become the new registrations' credentials. [Microsoft app registration guidance](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app).

## 6. Values: what goes where

| Value | Obtain from | Destination | Secret? |
|---|---|---|---|
| TenantSubdomain | Customer tenant primary domain prefix | All backend hosts; authorization hostname | No |
| TenantId | Customer tenant Overview | All backend hosts; OAuth endpoint path | No |
| Audience | DayNex API Application (client) ID | All backend hosts | No |
| RequiredScope | DayNex API exposed scope name | All backend hosts: access_as_user | No |
| Postman client ID | DayNex Postman Overview | Postman OAuth Client ID | No |
| Postman secret Value | DayNex Postman Certificates & secrets | Postman Vault/OAuth helper | Yes |
| Google client ID/secret | Google OAuth client | Entra Google provider | Secret part: yes |
| Callback URL | Postman OAuth helper | Entra Postman Web redirect; Postman | No |
| SQL connection string | Each service's database credentials | Its API User Secrets / deployment configuration | Yes |
| Access token | Entra token response | Bearer header to DayNex | Yes |

Backend template, added separately to Gateway, Identity API, and Holiday API:

```json
"Entra": {
  "TenantSubdomain": "YOUR_CUSTOMER_SUBDOMAIN",
  "TenantId": "YOUR_CUSTOMER_TENANT_GUID",
  "Audience": "YOUR_DAYNEX_API_CLIENT_GUID",
  "RequiredScope": "access_as_user"
}
```

The OAuth URLs are:

```text
https://SUBDOMAIN.ciamlogin.com/TENANT_GUID/oauth2/v2.0/authorize
https://SUBDOMAIN.ciamlogin.com/TENANT_GUID/oauth2/v2.0/token
```

The configuration Authority is a different URL used for discovery:

```text
https://SUBDOMAIN.ciamlogin.com/TENANT_GUID/v2.0
```

## 7. Tokens and protocols

OAuth 2.0 governs delegated API access. OpenID Connect adds sign-in identity semantics. Our client uses Authorization Code with PKCE: an authorization request carries a challenge; the token exchange supplies the matching verifier. The returned code is temporary and is not an API bearer token. Confidential-client authentication and PKCE protect different parts of the exchange, so our Web test client uses both. [Authorization code protocol](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow).

| Item | Used for | DayNex API accepts it? |
|---|---|---|
| Authorization code | Exchange for tokens | No |
| ID token | Client's sign-in identity | No |
| Access token for DayNex | Calling DayNex API | Yes, after validation and authorization |
| Microsoft Graph access token | Calling Graph | No |
| Refresh token, if issued | Obtaining replacement tokens at Entra | No |

`openid` requests OpenID sign-in, `profile` requests profile information, and `offline_access` requests refresh-token capability. These do not replace the DayNex API scope. The complete Postman request is `openid profile offline_access api://API_GUID/access_as_user`.

An ID token's audience identifies the client; our API expects its own resource audience. A JWT can be decoded without validating it, and its payload is not automatically confidential. Treat bearer credentials as secrets and let the resource validate them. [Access tokens](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens), [ID tokens](https://learn.microsoft.com/en-us/entra/identity-platform/id-tokens).

## 8. What the backend actually validates

`DayNex.Shared.Http/Authentication/AuthenticationDependencies.cs` configures ASP.NET Core JwtBearer. It uses Entra HTTPS discovery and middleware-managed signing keys; DayNex does not possess Microsoft's signing private key. Middleware caches metadata/key information rather than calling Entra for every request; initial discovery and refresh still need availability.

| Check in current code | Why it matters |
|---|---|
| Signed token and signing-key validation | Prevent fabricated tokens |
| RS256 allowed algorithm | Enforce the expected signing algorithm |
| Issuer | Trust the configured authority's issuer metadata |
| Audience GUID | Accept credentials intended for our API |
| Expiration/lifetime, expiration required | Reject expired or unbounded credentials |
| 30-second clock skew | Small allowance for clock differences |
| ver equals 2.0 | Enforce our supported token format |
| tid equals configured tenant | Enforce customer directory boundary |
| Valid, nonempty oid and tid | Build a stable verified customer key |
| Exact access_as_user entry in scp | Require delegated API access |

`MapInboundClaims = false` keeps the original claim names. The policy splits the space-separated `scp` value and matches an exact word; it does not use substring matching. A token missing the delegated scope cannot pass the customer policy. Public `/health` is a deliberate exception.

The API token requirements are our current design. We have not enabled arbitrary issuers, other token versions, or an app-only background-worker authorization path.

## 9. Gateway versus API protection

The gateway validates authentication and scope before forwarding protected Ocelot routes. Ocelot routes run as middleware; we explicitly execute the policy there instead of assuming MVC authorization automatically covers them.

Each API independently validates the same token. This protects direct API access as well as gateway access. The Holiday API additionally requires `holidays.sync`. The gateway does not itself make the business-role database decision.

All current hosts share one API audience. This allows Holiday's permission client to forward the same token to Identity. It is a deliberate small-system tradeoff: less audience isolation and a synchronous permission dependency. If services later have distinct resource audiences, blindly forwarding this token is wrong; delegated token exchange or an explicitly designed service authorization model would be required.

## 10. IdentityService and first-use provisioning

IdentityService is a business identity/profile service, not our OAuth authorization server. It does not mint customer tokens or handle Google passwords.

`IdentityController` takes identity from the verified principal. `ExternalIdentity` forms `ExternalId = tid:oid`, with optional email/name. Query parameters cannot choose a different user. Email is not used for automatic account linking because it can change or collide. The stable key is stable for the object's lifetime, not after deleting and recreating the directory user. [Microsoft claim reference](https://learn.microsoft.com/en-us/entra/identity-platform/access-token-claims-reference).

`GET /api/identity/me` calls `UserProfileService.GetOrProvisionAsync`. Existing users are loaded; absent users get User role and a Free subscription. Permissions access alone does not provision and returns 404 for an absent profile. Profile responses are marked no-store.

First-use creation through GET is our present implementation convenience. For a mature API contract, an explicit idempotent provisioning POST would align better with HTTP's safe-method semantics. This is a design tradeoff to explain, not a reason to pretend it is already changed.

## 11. Database concurrency and subscriptions

`IdentityStore.FindAsync` uses one indexed, no-tracking query joining the user with an optional subscription. `ProvisionAsync` inserts both entities in one atomic SaveChanges operation.

Two simultaneous first requests can both observe no user. The unique ExternalId index is the final arbiter across processes. The losing insertion catches SQL uniqueness errors 2601/2627, clears tracked state, and rereads the winning account. A process-local lock would not protect multiple API replicas.

The subscription FK prevents orphan records. Email uniqueness was removed and email made optional. Expired subscriptions evaluate as Free. Current code reads existing profile information; it does not automatically overwrite profile fields from every new token.

## 12. Business authorization and HolidayService

`UserProfileService.GetPermissionsAsync` maps database Admin/SuperAdmin roles to `holidays.sync`. Premium does not grant that permission. No public endpoint lets a customer promote themselves.

Holiday's `IdentityPermissionClient` calls Identity's self-only permissions endpoint over HTTPS, forwarding the current bearer token. Redirects are disabled, timeout is five seconds, and credentials are not sent to GOV.UK. The scoped permission handler does at most one lookup per request and avoids stale cross-request permission caching.

Missing permission blocks sync. A permission-service outage produces 503 rather than granting access. This improves revocation freshness but adds latency and an availability dependency. A request already authorized can still finish after a subsequent role revocation; this is not instantaneous cancellation of in-flight work.

The Holiday Application layer defines its provider interface. Infrastructure implements it using the generic shared HTTP client. Shared transport mechanics do not replace service-specific business contracts.

## 13. Code ownership by layer

| Project/layer | Responsibility and relevant files |
|---|---|
| Shared.Http Authentication | EntraOptions, AuthenticationDependencies, ExternalIdentity |
| Shared.Http Authorization | IPermissionSource, IdentityPermissionClient, PermissionAuthorizationHandler |
| Shared.Http Hosting/HTTP | Common host safeguards and provider HTTP resilience |
| Gateway | Program.cs authentication/scope middleware; ocelot.json routing |
| Identity WebApi | IdentityController: verified self profile and permissions endpoints |
| Identity Application | UserProfileService and IIdentityStore: use cases and abstractions |
| Identity Domain | User, Subscription, role/tier invariants |
| Identity Infrastructure | EF IdentityStore, database configuration, migrations |
| Holiday Application | Sync orchestration and own provider/store interfaces |
| Holiday Infrastructure | GOV.UK client implementation and transactional catalogue persistence |
| Holiday API | Authenticated administrator sync endpoint |

`EntraOptions` and database settings use sealed classes to prevent inheritance of configuration types. Sealed does not encrypt data, make properties immutable, or itself provide a security boundary. Options binding and startup validation provide typed configuration and early detection of missing values. These concerns belong in host/shared infrastructure rather than Domain business entities.

One solution contains multiple independently hosted API projects. A solution is a development grouping; independent process/deployment and ownership boundaries determine whether these are microservices. Shared business models, common release constraints, or mandatory shared deployment can still create distributed-monolith coupling.

## 14. Secrets, Azure SQL, and resilience

Local API connection strings were moved out of tracked appsettings into API User Secrets. User Secrets is development storage outside the repository, not an encrypted production vault. Environment values can override secrets. Each API must point to its own database. See the existing authentication guide for migration commands.

Runtime SQL retries use EnableRetryOnFailure(3). Design-time factories read API User Secrets/environment configuration and now have five retries with a ten-second maximum retry delay. Transient retries do not repair wrong credentials, firewall rules, or unavailable quota. SSMS success proves one connection at that time; it does not prove the API uses identical settings.

EF database commands need the correct Infrastructure project and API startup project. Using Gateway as startup led to the missing Infrastructure DLL error. Runtime does not automatically apply migrations.

Shared provider HTTP has pooled connections, bounded responses, a 30-second total timeout, 10-second attempt timeout, two jittered retries, and a resilience circuit breaker. Automatic retries are disabled for unsafe methods. Holiday fetch happens before its short serializable database transaction; a unique natural key prevents duplicate catalogue entries.

## 15. Production delivery: work still needed

These are next delivery requirements, not claims of existing deployment:

1. Run the real-token acceptance checks in section 16.
2. Select Azure hosting after checking current free quotas, runtime support, cold starts and cost. Free identity allowance does not imply free hosting, networking, logs, or SQL at any scale.
3. Set each host's Entra configuration and service-specific secrets through deployment configuration. Prefer a managed identity for supported Azure resource access where practical; SQL authentication currently remains connection-string based.
4. Managed identity identifies the running workload to Azure, not the Google customer. Configure SQL data-plane users/permissions as well as networking; an Azure management role alone does not grant SQL query access. [Managed identity architecture](https://learn.microsoft.com/en-us/entra/architecture/service-accounts-managed-identities).
5. Replace localhost downstream routes and IdentityService BaseUrl with actual reachable HTTPS addresses. Restrict internal service ingress where the hosting plan supports it.
6. Configure trusted reverse proxies, HTTPS, and a production certificate path. Add frontend CORS origins when a frontend exists; CORS is not authentication.
7. Add CI build/tests, reviewed migration deployment, configuration validation, rollback procedure, and resource backup/restore exercises.
8. Monitor sign-in failures, API 401/403/503 rates, latency, SQL saturation and upstream failures without logging tokens/secrets. Current /health is liveness, not database readiness.
9. Coordinate rate limiting across replicas. Current limits are local: 120 requests/minute by identity/IP and two syncs/minute per identity, per instance.
10. Design frontend session/token storage, refresh and logout with an appropriate supported identity library. There is currently no frontend and no DayNex refresh-token store.
11. Add an audited administration process, account deletion/data-retention process, and measurable capacity targets.

External ID Basic currently includes the first 50,000 monthly active users at no cost. This is monthly unique authentication activity, not 50,000 concurrent sessions. Add-ons have separate charges and billing aggregation rules; verify subscription linkage and current pricing before release. [Microsoft pricing](https://www.microsoft.com/en-us/security/pricing/microsoft-entra-external-id/).

Logout/identity disablement does not make locally validated bearer tokens disappear immediately. Our code does not implement per-request session revocation checks. Plan acceptable access-token lifetime and any application-specific suspension mechanism.

## 16. End-to-end acceptance checklist

| Action | Expected evidence |
|---|---|
| Obtain token through Google | Postman displays a successful token response |
| Gateway GET /api/identity/me with token | 200 and verified profile |
| Repeat profile request | Same user ID; one user/subscription in SQL |
| Profile without token, through gateway and directly | 401 |
| Profile with altered, expired or wrong-audience token | 401 |
| Valid authenticated identity lacking API scope | 403 |
| GET self permissions after provisioning | 200; normal user has no holidays.sync |
| Normal user POST /api/holidays/sync | 403 |
| Controlled Admin test account sync | 200; persisted catalogue |
| Repeat sync | Existing rows updated, no duplicate natural keys |
| Permission service unavailable | Sync blocked; 503 for service outage |
| Admin role revoked | Subsequent permission evaluation denies sync |

Do not paste real tokens into shared artifacts. Use the collection and environment under the postman directory. Automated tokens are test-only; production validation remains Entra-backed.

## 17. Troubleshooting from our actual session

| Symptom | Cause/lesson |
|---|---|
| Customer app absent from flow | Registrations were in the other tenant; recreate them in DAYNEX USERS |
| My APIs empty | Verify tenant, exposed scope, app owners; search APIs my organization uses |
| Invalid host / literal {{variables}} | Environment not selected or values missing; unresolved text reached the request |
| /auth URL returned 404 | Correct endpoint is /authorize; remove placeholder hostname |
| AADSTS650053, bare GUID scope on Graph | Requested malformed scope; copy full API_URI/access_as_user |
| AADSTS50011 | Requested callback did not exactly match the client's registered Web redirect |
| Browser authenticated but no return to Postman | Browser pop-up/app handoff blocked |
| AADSTS7000215 | Wrong secret Value, wrong app's secret, expired secret, or unresolved secret reference |
| Token succeeds but backend 401 | Check API audience, tid, oid, ver, signature/lifetime and host settings |
| Metadata DLL missing during EF | Fix compilation or wrong startup project; do not copy DLLs into Gateway |
| Azure SQL 40613 | Temporary unavailability/resume observed; retries helped, but inspect underlying error |

## 18. Interview questions and explained answers

### 1. Explain DayNex authentication in one minute.

“I registered a customer API and a separate test client in an Entra external tenant, enabled Google federation, and associated the client with a user flow. The client uses authorization code with PKCE to obtain an Entra access token. Gateway and APIs validate it. IdentityService provisions business profiles from verified claims, while SQL roles determine administrator operations.” This describes both the identity provider and the application boundary.

### 2. Why use an external tenant rather than Default Directory?

DayNex serves consumers who self-register. The external customer tenant is the configured customer identity boundary; Default Directory remains our administration context. B2B guest collaboration is a different requirement. Choosing based on the user population is better than choosing based on where SQL happens to live.

### 3. Are tenant ID, client ID, object ID and audience the same?

No. Tenant ID identifies the directory; client ID identifies an application registration; object ID identifies a directory object; audience identifies the token's intended resource. In our backend Audience is the API client GUID. Mixing these values caused real configuration errors.

### 4. Why two Entra app registrations?

The client requests access; the API receives it. They have different audiences, callbacks, and credential responsibilities. Reusing a client ID in the resource Audience would accept the wrong intended recipient or reject correct API tokens.

### 5. Does Google issue the token accepted by DayNex?

No. Google handles federated authentication; Entra issues our resource token. The backend trusts one configured Entra authority instead of maintaining separate Google-token validation paths.

### 6. Why not implement our own JWT issuer?

We would take responsibility for credential handling, signing-key rotation, federation and session lifecycle. We delegated authentication to Entra and retained application authorization in our database. Ownership boundaries matter more than having fewer dependencies.

### 7. What does PKCE protect, and why also a client secret?

The verifier binds code redemption to the initiating client exchange. Our Web test registration also authenticates as a confidential client. PKCE does not make it safe to embed a static secret in an Android or SPA application.

### 8. What is the difference between scope and role?

access_as_user is delegated API permission. Our database Admin role maps to holidays.sync. A token can carry API scope while its user is forbidden to run sync. This separates permission to call the resource from permission to perform a particular business operation.

### 9. Does granting admin consent make every customer an administrator?

No. Tenant consent approves the client permission grant; it does not change DayNex database roles. New users remain User/Free. Confusing these would introduce privilege escalation.

### 10. Why reject an ID token?

Its purpose and audience belong to the signing-in client. DayNex requires a resource access token with API audience and delegated scope. Successful sign-in alone is not authorization to any API.

### 11. How do you securely validate JWTs?

Use maintained middleware and authority discovery; require trusted signature/algorithm, issuer, audience, expiry, then enforce required identity claims and scope. Decoding payload JSON is not validation. Our implementation adds tenant, version and stable-identity checks to middleware validation.

### 12. Why validate at the gateway and each service?

The gateway rejects invalid traffic early. Service validation protects direct access and route mistakes. The extra local validation cost is modest; scope and operation policies still belong at appropriate boundaries.

### 13. Does every API request call Entra?

No: JWT validation uses cached discovery/signing information. Initial metadata retrieval and refresh are still network-dependent. Our Holiday permission check separately calls Identity per request; distinguish those dependencies.

### 14. Why identify users with tid:oid rather than email?

Email is mutable profile data, not our authorization key. Tenant plus directory object ID provides the current object's stable identity boundary. Automatic email-based linking could connect unrelated accounts. Deleted/recreated identities need explicit reconciliation.

### 15. How do you prevent duplicate users under concurrency?

A SQL unique index arbitrates concurrent first requests across replicas. User and subscription inserts are atomic; uniqueness losers reread the winner. This remains correct when application instances do not share memory.

### 16. What tradeoff comes with just-in-time provisioning?

It avoids an unnecessary pre-registration backend step but adds a write and concurrency path on first profile access. Our current GET has that side effect; an explicit idempotent POST would improve HTTP semantics. Be candid about the implemented contract.

### 17. Why query permissions instead of storing Admin in long-lived tokens?

Database lookup makes subsequent authorization reflect role changes without waiting for role claims to expire. It costs a synchronous dependency and adds latency. Per-request memoization reduces duplicate lookups without stale cross-request grants.

### 18. What happens when IdentityService is down?

Administrator sync fails closed and reports 503 when permission retrieval is unavailable. We do not assume permission from a premium tier or allow work because the check failed. Availability and security tradeoffs are explicit.

### 19. Is forwarding the customer bearer token safe between services?

Only to a trusted intended recipient over protected transport. Our services share the resource audience and the permission call targets Identity's self endpoint; redirects are disabled. With distinct audiences, token forwarding would need redesign. Never send that token to GOV.UK.

### 20. What would you change as the service count grows?

Reevaluate separate API audiences, independent deployment, authorization dependencies, distributed limits and observability. Prefer smaller independently owned contracts. Do not promise that splitting repositories itself creates independence.

### 21. Why share HTTP infrastructure?

Pooling, timeouts, retries and error handling are cross-cutting transport concerns. Provider-specific contracts and business rules remain owned by each service. A shared library needs version discipline so releases do not force every service to move together.

### 22. Why not retry every failure?

Permanent configuration/auth failures will not improve with repetition; writes can create duplicate side effects. Our provider retries exclude unsafe methods. SQL transaction replay is coordinated through EF execution strategy and database constraints rather than arbitrary nested retries.

### 23. What is the difference between managed identity and customer identity?

Managed identity represents an Azure workload when it accesses supported resources. Customer identity represents the signed-in human using DayNex. They require distinct tokens, audiences and permissions. Managed SQL identity is a future deployment improvement here, not already configured.

### 24. How do you protect configuration?

Keep credential values out of tracked files; use development User Secrets locally and controlled deployment secret/configuration storage. Validate typed options at startup, limit access, rotate exposed credentials and avoid logging them. User Secrets and sealed classes are not encryption mechanisms.

### 25. What is your testing strategy?

Signed-token HTTP tests exercise authentication and policy failures; relational tests check transactions and constraints; disposable SQL Server tests exercise real uniqueness races and migrations. Real Entra/Google tests validate portal integration. Load tests are a separate category and have not yet established capacity.

### 26. What distinguishes 401, 403 and 503?

401 means acceptable authentication is absent; 403 means the authenticated caller lacks a required authorization condition; 503 means a required service is unavailable. Correct status codes improve diagnosis and prevent clients treating outages as account problems.

### 27. How would you deliver this end to end?

Agree acceptance criteria, configure identity and resources, implement boundaries, automate tests and deployments, review migrations, secure environment configuration, run real integration checks, and establish monitoring/rollback. Finish with evidence against the criteria, not just a successful local build.

### 28. What risks do you communicate to a product owner?

Free quotas/cold starts affect service availability; permission checks add dependency; shared audience reduces isolation; per-instance rate limiting does not coordinate replicas; production frontend sessions and account lifecycle are outstanding. Explain practical effects and options rather than claiming enterprise readiness from unit tests.

### 29. How do logout and account disablement affect access tokens?

Clearing the client session stops ordinary client use but does not invalidate a stolen, unexpired JWT by itself. Current local validation has no per-request revocation check. Define expiration and business suspension policy for the product's risk level.

### 30. What would you improve before calling this production-ready?

Complete live API verification, establish audited role administration, implement frontend session lifecycle, review the provisioning GET, deploy trusted ingress and secrets, add readiness/monitoring, test recovery and benchmark capacity. Present these as planned improvements; do not describe them as already shipped.

## 19. A truthful interview project summary

“I built a .NET 10 multi-service backend with a shared Entra validation component, independently protected API hosts, and SQL-owned customer authorization. I configured Google federation in an external customer tenant and verified real token acquisition with Postman. I implemented concurrent first-use provisioning with atomic database writes and uniqueness constraints, permission checks that fail closed, and bounded provider HTTP resilience. Automated tests cover authentication, authorization and database races. The remaining delivery work is real-token API acceptance, production hosting and frontend session lifecycle; I have not yet benchmarked the free-tier deployment.”

After the remaining acceptance checks pass, update that summary with the actual evidence. The click-by-click setup companion is [Entra-Login-Visual-Guide.html](Entra-Login-Visual-Guide.html); the operational reference is [Authentication.md](Authentication.md).

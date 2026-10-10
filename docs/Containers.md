# Local DayNex containers

From the backend root, run `./tools/Start-LocalContainers.ps1`. Add `-Build` after changing application code.
The script reads the existing Identity and Holiday Windows User Secrets into temporary process environment variables. It does not print them or write a credentials file. Docker retains runtime environment values in container metadata; local Docker access is privileged.

| Service | Local endpoint |
| --- | --- |
| Gateway | http://localhost:18080 |
| Identity | http://localhost:18081 |
| Holiday | http://localhost:18082 |

All ports bind only to Windows loopback. This is a Development-only HTTP setup. Do not use these Compose settings for public hosting.
Ocelot's local route file is mounted read-only from `deploy/local/ocelot.json`; the existing Visual Studio routes stay intact.
Within the Compose network, Gateway uses `identity:8080` and `holiday:8080`; Holiday's permission client uses `identity:8080`.
The permission client permits non-loopback HTTP only when ASPNETCORE_ENVIRONMENT is Development. Production service addresses must use HTTPS.

Health endpoints verify application startup, not database connectivity. Test `/api/identity/me` and `/api/holidays/upcoming?region=EnglandAndWales` through Gateway with a current DayNex Entra access token to verify authenticated database operations.
An ordinary customer must be forbidden from POST `/api/holidays/sync`.

For Azure, deploy images to separate Container Apps, configure internal HTTPS downstream hostnames and runtime secrets/identities, and expose only Gateway publicly. This local setup does not create Azure resources or apply migrations.
## Detailed learning documents

Open E:\Projects\DayNex\Documents\index.html for the delivery learning library.
The complete container guide is 01-DayNex-Local-Containers-Learning-Guide.html.
The user's documentation preference is to create a numbered, detailed HTML guide in that folder after each completed delivery step, including labelled visuals, reproducible instructions, verification, troubleshooting and interview Q&A. See Documents/README.md for the recorded workflow.
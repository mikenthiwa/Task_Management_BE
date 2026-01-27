# Task_Management_BE

## Local Development
- Restore packages and run the API with hot reload:
  ```bash
  dotnet watch run --project src/Web
  ```
- Set the development connection string via user secrets (required because `appsettings.Local.json` has no password):
  ```bash
  cd src/Web
  dotnet user-secrets init
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Username=postgres;Password=<your-password>;Database=TaskLocalDb"
  dotnet user-secrets set "Cors:AllowedOrigins" "http://localhost:3000"
  dotnet user-secrets set "CLOUDINARY_URL" "cloudinary://<api_key>:<api_secret"@<cloud_name>"
  dotnet user-secrets set "Cloudinary:CloudName" "<cloud_name>"
  dotnet user-secrets set "Cloudinary:ApiKey" "<api_key>"
  dotnet user-secrets set "Cloudinary:ApiSecret" "<api_secret>"
  dotnet user-secrets set "WorkerApiKey" "<shared-worker-key>"
  ```

- NB: BUILD RABBITMQ ONLY (DURING TESTING FOR DEVELOPMENT ONLY):*
  ```bash
  docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml up -d rabbitmq
    ```

## Notification Worker (SignalR publishing via web API)
- The worker publishes notifications by calling the web API endpoint `POST /api/NotificationsInternal/internal/notifications`.
- Configure these settings for both the web app and the worker:
  - `WorkerApiKey`: shared secret sent in the `X-Worker-Key` header.
  - `WebBaseUrl`: base URL for the web app (worker only), e.g. `http://localhost:5000/`.
- Example worker user secrets:
  ```bash
  cd src/NotificationWorker
  dotnet user-secrets init
  dotnet user-secrets set "WebBaseUrl" "http://localhost:5230/"
  dotnet user-secrets set "WorkerApiKey" "<shared-worker-key>"
  ```
- Run the worker:
  ```bash
  dotnet run --project src/NotificationWorker
  ```

## Docker
- Copy the sample environment file and update the secrets:
  ```bash
  cp .env.example .env
  # edit .env and set POSTGRES_PASSWORD and API_CONNECTION_STRING as needed
  ```
- Set `ALLOWED_ORIGINS` in `.env` using a semicolon-separated list (e.g. `http://localhost:3000;https://app.example.com`). The API reads `Cors:AllowedOrigins` from that environment variable when running in Docker.
- Build and run the stack (API + Postgres):
  ```bash
  docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml up --build
  ```
- The API listens on `http://localhost:8080`. The container uses the connection string supplied in `.env`.
- If you change service ports or credentials, update `.env` and rerun `docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml up`.

## Database Lifecycle
- In development the database initializer runs when `ASPNETCORE_ENVIRONMENT=Development`. Inside Docker it connects to the compose Postgres instance without dropping the schema.
- For persistent environments generate migrations with:
  ```bash
  dotnet ef migrations add <MigrationName> --project src/Infrastructure --startup-project src/Web
  ```
  Apply them via `dotnet ef database update` or `Database.MigrateAsync()` instead of relying on `EnsureDeleted/EnsureCreated`.
- To seed a default administrator outside development, set `SeedData__Enabled=true` and `SeedData__Admin__Password=<strong-password>` (App Service settings, environment variables, or `appsettings.{Environment}.json`). The initializer creates the admin only if those values are supplied.
- Configure CORS origins via configuration. Example JSON:
  ```json
  "Cors": {
    "AllowedOrigins": "http://localhost:3000;https://app.example.com"
  }
  ```
  Equivalent environment variable (user secrets, App Service settings, or Docker environment):  
  `Cors__AllowedOrigins="http://localhost:3000;https://app.example.com"`

## Azure Container Deployment
- Authenticate with Azure and your container registry(taskmanagementregistry.azurecr.io):
  ```bash
  az login
  az acr login -n <registry-name>
  ```
- Build and tag the Docker image using the registry login server (publish as 64-bit Linux for Azure):
  ```bash
  docker buildx build --platform linux/amd64 \
    -t <registry-name>.azurecr.io/task-management-be:<environment> .
  ```
- Push the image to Azure Container Registry:
  ```bash
  docker push <registry-name>.azurecr.io/task-management-be:<environment>
  ```
- Configure your hosting target (App Service, Container Apps, or Container Instances) to pull that image and supply required settings via environment variables:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ConnectionStrings__DefaultConnection=<azure-postgres-connection-string>`
  - `SeedData__Enabled=true` (optional, run once to seed the admin user)
  - `Cors__AllowedOrigins=http://localhost:3000;https://app.example.com`
- After the first successful start, disable seeding by setting `SeedData__Enabled=false` so the admin isn’t recreated on every restart.

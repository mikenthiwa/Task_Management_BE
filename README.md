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
  dotnet user-secrets set "RabbitMQ:HostName" "<rabbitmq-host>"
  dotnet user-secrets set "RabbitMQ:UserName" "<rabbitmq-username>"
  dotnet user-secrets set "RabbitMQ:Password" "<rabbitmq-password>"
  dotnet user-secrets set "Caching:Redis:ConnectionString" "localhost:6379,abortConnect=false"
  ```

- NB: BUILD RABBITMQ ONLY (DURING TESTING FOR DEVELOPMENT ONLY):*
  ```bash
  docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml up -d rabbitmq
    ```

## Tests
- Run all tests:
  ```bash
  dotnet test
  ```
- Run functional tests only:
  ```bash
  dotnet test tests/Application.FunctionalTests/Application.FunctionalTests.csproj
  ```

## Notification Worker (SignalR publishing via web API)
- Notification dispatch is controlled by `Notifications:DispatchMode`.
- Supported values:
  - `InProcess`: the API stores notifications and pushes SignalR updates directly.
  - `RabbitMq`: the API publishes notification events to RabbitMQ, and the worker consumes them.
- Default behavior:
  - `Development` defaults to `RabbitMq`.
  - Non-development environments default to `InProcess`.
- The worker is only required when `Notifications:DispatchMode=RabbitMq`.
- When RabbitMQ mode is enabled, the worker publishes notifications by calling the web API endpoint `POST /api/NotificationsInternal/internal/notifications`.
- Configure these settings for both the web app and the worker:
  - `WorkerApiKey`: shared secret sent in the `X-Worker-Key` header.
  - `WebBaseUrl`: base URL for the web app (worker only), e.g. `http://localhost:5000/`.
- Example worker user secrets:
  ```bash
  cd src/NotificationWorker
  dotnet user-secrets init
  dotnet user-secrets set "WebBaseUrl" "http://localhost:5230/"
  dotnet user-secrets set "WorkerApiKey" "<shared-worker-key>"
  dotnet user-secrets set "RabbitMQ:HostName" "<rabbitmq-host>"
  dotnet user-secrets set "RabbitMQ:UserName" "<rabbitmq-username>"
  dotnet user-secrets set "RabbitMQ:Password" "<rabbitmq-password>"
  dotnet user-secrets set "Caching:Redis:ConnectionString" "localhost:6379,abortConnect=false"
  ```
- Run the worker:
  ```bash
  dotnet run --project src/NotificationWorker
  ```
- RabbitMQ settings for the worker (optional; defaults to `localhost/admin/admin`):
  - `RabbitMq:HostName`
  - `RabbitMq:UserName`
  - `RabbitMq:Password`
  - `RabbitMq:VirtualHost`
  - `RabbitMq:Port`
  - `RabbitMq:UseSsl`

## Docker
- Copy the sample environment file and update the secrets:
  ```bash
  cp .env.example .env
  # edit .env and set POSTGRES_PASSWORD and API_CONNECTION_STRING as needed
  ```
- Set `ALLOWED_ORIGINS` in `.env` using a semicolon-separated list (e.g. `http://localhost:3000;https://app.example.com`). The API reads `Cors:AllowedOrigins` from that environment variable when running in Docker.
- Development compose explicitly sets `Notifications__DispatchMode=RabbitMq` to keep the RabbitMQ + worker flow available locally.
- Production compose explicitly sets `Notifications__DispatchMode=InProcess` so notification creation runs inside the API process.
- To enable distributed caching with Redis, set the connection string (example uses local Redis):
  - `Caching__Redis__ConnectionString=localhost:6379,abortConnect=false`
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

## Heroku Container Deployment

This application can be deployed to Heroku as one or two Docker process types:

- `web`: ASP.NET Core API and SignalR hub.
- `worker`: optional notification worker that consumes RabbitMQ messages and calls the internal notification API.

For the cheaper production path, use:

```text
Notifications__DispatchMode=InProcess
```

With this mode, the `worker` dyno and RabbitMQ are not required for task notifications.

For development or event-driven testing, use:

```text
Notifications__DispatchMode=RabbitMq
```

With this mode, release and scale the `worker` process too.

### 1. Create and Prepare the Heroku App

If the app does not exist yet:

```bash
heroku create <app-name>
```

Set the Heroku stack to container:

```bash
heroku stack:set container -a <app-name>
```

Login to the Heroku container registry:

```bash
heroku container:login
```

### 2. Required Heroku Add-ons

Provision or attach:

- Heroku Postgres
- Heroku Redis
- CloudAMQP

Heroku add-ons usually provide these URL-style config vars automatically:

```text
DATABASE_URL
REDIS_URL
CLOUDAMQP_URL
```

The application supports those as fallback values through `AddHerokuAddonConfiguration()`. The preferred production contract is structured .NET configuration using `__` separators.

### 3. Recommended Production Config Vars

Use structured config vars where possible:

```bash
heroku config:set \
  ASPNETCORE_ENVIRONMENT=Production \
  ConnectionStrings__DefaultConnection="<postgres-connection-string>" \
  Caching__Redis__ConnectionString="<redis-connection-string>" \
  Caching__Redis__SkipCertificateValidation=true \
  RabbitMq__HostName="<rabbitmq-host>" \
  RabbitMq__UserName="<rabbitmq-user>" \
  RabbitMq__Password="<rabbitmq-password>" \
  RabbitMq__VirtualHost="<rabbitmq-virtual-host>" \
  RabbitMq__Port=5671 \
  RabbitMq__UseSsl=true \
  Notifications__DispatchMode=InProcess \
  WebBaseUrl="https://<app-name>.herokuapp.com/" \
  WorkerApiKey="<shared-worker-key>" \
  Cors__AllowedOrigins="http://localhost:3000;https://your-frontend.example.com" \
  -a <app-name>
```

If structured database, Redis, or RabbitMQ settings are absent, the app falls back to:

- `DATABASE_URL` -> `ConnectionStrings:DefaultConnection`
- `REDIS_URL` -> `Caching:Redis:ConnectionString`
- `CLOUDAMQP_URL` -> `RabbitMq:*`

RabbitMQ settings are only required by the web app when `Notifications__DispatchMode=RabbitMq`. They are still required by the worker if the worker process is deployed.

### 4. Build and Push Images

Heroku does not accept all Docker Buildx image indexes. Build a single linux/amd64 image with provenance and SBOM disabled, then load it locally before pushing.

Build and push the web image:

```bash
docker buildx build \
  --platform linux/amd64 \
  --target web \
  --provenance=false \
  --sbom=false \
  --load \
  -t registry.heroku.com/<app-name>/web .

docker push registry.heroku.com/<app-name>/web
```

Build and push the worker image:

```bash
docker buildx build \
  --platform linux/amd64 \
  --target worker \
  --provenance=false \
  --sbom=false \
  --load \
  -t registry.heroku.com/<app-name>/worker .

docker push registry.heroku.com/<app-name>/worker
```

### 5. Release and Scale Processes

Release the web process:

```bash
heroku container:release web -a <app-name>
```

For RabbitMQ mode, release both process types:

```bash
heroku container:release web worker -a <app-name>
```

Scale the production in-process path:

```bash
heroku ps:scale web=1 worker=0 -a <app-name>
```

Scale the RabbitMQ worker path:

```bash
heroku ps:scale web=1 worker=1 -a <app-name>
```

The worker is required only for RabbitMQ-backed notifications. If `Notifications__DispatchMode=RabbitMq` and only `web` is running, queued task notification events will not be consumed.

### 6. Verify Deployment

Check dynos:

```bash
heroku ps -a <app-name>
```

Expected process types for in-process mode:

```text
web.1: up
```

Expected process types for RabbitMQ mode:

```text
web.1: up
worker.1: up
```

Check health:

```bash
curl https://<app-name>.herokuapp.com/api/health/live
curl https://<app-name>.herokuapp.com/api/health
```

The ready health endpoint should report healthy PostgreSQL and Redis checks. RabbitMQ is included in health checks only when `Notifications__DispatchMode=RabbitMq`.

Check logs:

```bash
heroku logs -n 100 -a <app-name>
```

For realtime notifications, confirm:

- SignalR connects to `/notificationHub`.
- The frontend registers `ReceiveNotification` before starting the hub connection.
- The hub is authenticated with a fresh access token.
- The worker logs successful calls to `/api/NotificationsInternal/internal/notifications`.

## Azure Container Deployment
- Authenticate with Azure and your container registry(taskmanagementregistry.azurecr.io):
  ```bash
  az login
  az acr login -n <registry-name>
  ```
- Build, tag, and push the Docker image using the registry login server (publish as 64-bit Linux for Azure):
  ```bash
  docker buildx build --platform linux/amd64 -t <registry-name>.azurecr.io/task-management-worker:<tag> --target worker --push .
  
  docker buildx build --platform linux/amd64 -t taskmanagementregistry.azurecr.io/task-management-api:dev --target web --push .
  ```

- Configure your hosting target (App Service, Container Apps, or Container Instances) to pull that image and supply required settings via environment variables:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ConnectionStrings__DefaultConnection=<azure-postgres-connection-string>`
  - `SeedData__Enabled=true` (optional, run once to seed the admin user)
  - `Cors__AllowedOrigins=http://localhost:3000;https://app.example.com`
- After the first successful start, disable seeding by setting `SeedData__Enabled=false` so the admin isn’t recreated on every restart.

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
  ```

## Docker
- Copy the sample environment file and update the secrets:
  ```bash
  cp .env.example .env
  # edit .env and set POSTGRES_PASSWORD and API_CONNECTION_STRING as needed
  ```
- Build and run the stack (API + Postgres):
  ```bash
  docker compose up --build
  ```
- The API listens on `http://localhost:8080`. The container uses the connection string supplied in `.env`.
- If you change service ports or credentials, update `.env` and rerun `docker compose up`.

## Database Lifecycle
- In development the database initializer runs when `ASPNETCORE_ENVIRONMENT=Development`. Inside Docker it connects to the compose Postgres instance without dropping the schema.
- For persistent environments generate migrations with:
  ```bash
  dotnet ef migrations add <MigrationName> --project src/Infrastructure --startup-project src/Web
  ```
  Apply them via `dotnet ef database update` or `Database.MigrateAsync()` instead of relying on `EnsureDeleted/EnsureCreated`.

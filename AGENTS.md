# Repository Guidelines

## Project Structure & Module Organization
This repository follows a layered .NET backend layout:
- `src/Domain`: core entities, enums, constants, and domain events; keep it free of infrastructure concerns.
- `src/Application`: use cases, validators, DTOs, interfaces, and feature folders such as `Features/Tasks/Command/CreateTask` and `Features/Tasks/Queries`.
- `src/Infrastructure`: EF Core data access, identity, Redis, RabbitMQ, background workers, external services, and migrations.
- `src/Web`: HTTP API endpoints, middleware, health checks, dependency injection, and app configuration.
- `src/NotificationWorker`: notification processing worker service.
- `tests/Application.FunctionalTests`: xUnit functional tests for API and persistence behavior.
- Keep business logic in Domain/Application rather than controllers, endpoint shells, or infrastructure adapters.

## Build, Test, and Development Commands
- `dotnet restore` — restore solution dependencies.
- `dotnet build Task_Management_BE.sln` — build all projects; the repo targets `net10.0` and treats most warnings as errors.
- `dotnet watch run --project src/Web` — run the API locally with hot reload.
- `dotnet run --project src/NotificationWorker` — start the notification worker.
- `dotnet test` — run the full test suite.
- `dotnet test tests/Application.FunctionalTests/Application.FunctionalTests.csproj` — run functional tests only.
- `docker compose --env-file .env.development -f docker-compose.yml -f docker-compose.dev.yml up --build` — start the local container stack.

## Coding Style & Naming Conventions
Follow `.editorconfig` exactly:
- Use 4 spaces in `*.cs`; use 2 spaces in JSON, XML, and project files.
- Prefer file-scoped namespaces, nullable reference types, and implicit usings.
- Use PascalCase for types, methods, and properties; camelCase for locals; prefix interfaces with `I`.
- Prefer explicit types over `var` unless the existing file clearly uses a different pattern.
- Prefer async/await for I/O and avoid blocking calls.
- Keep feature code grouped by capability, for example `Features/Tasks/Command/CreateTask`.
- Preserve existing architectural and naming conventions, and avoid new dependencies unless they solve a clear project need.

## Testing Guidelines
Tests use xUnit, FluentAssertions, and `Microsoft.AspNetCore.Mvc.Testing`; some scenarios rely on Testcontainers for PostgreSQL.
- Add or update functional tests for meaningful endpoint, validation, persistence, authorization, or business-rule changes.
- Keep test names behavior-focused, e.g. `ShouldCreateTask` and `ShouldRequireMinimumFields`.
- Run relevant build and test commands before marking work complete, or state why they were not run.
- No enforced coverage threshold is configured; cover new behavior and regressions rather than chasing arbitrary percentages.

## Commit & Pull Request Guidelines
Recent history follows Conventional Commit prefixes such as `feat:`, `refactor:`, and `test:`. Keep commits focused and descriptive.

For pull requests, include:
- a short summary of the change and affected layers,
- linked issue or task reference,
- test evidence (`dotnet test`, targeted test command, or API verification),
- notes for config, migrations, or new secrets when applicable.
- Highlight risky, breaking, migration-related, or operationally sensitive changes before applying them.
- Show a proposed diff before large or multi-file changes.

## Security & Configuration Tips
Do not commit secrets. Use `dotnet user-secrets` for local development and keep `.env` values out of source control. Prefer PostgreSQL-compatible changes for relational data. When changing data models, create EF Core migrations with `dotnet ef migrations add <MigrationName> --project src/Infrastructure --startup-project src/Web`.

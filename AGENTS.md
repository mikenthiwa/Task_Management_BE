# Repository Guidelines

## Project Structure & Module Organization
This repository follows a layered .NET backend layout:
- `src/Domain`: core entities, enums, constants, and domain events.
- `src/Application`: use cases, validators, DTOs, interfaces, and feature folders such as `Features/Tasks/Command` and `Features/Tasks/Queries`.
- `src/Infrastructure`: EF Core data access, identity, Redis, RabbitMQ, background workers, and migrations.
- `src/Web`: the HTTP API, endpoint definitions, health checks, and app configuration.
- `src/NotificationWorker`: worker service for notification processing.
- `tests/Application.FunctionalTests`: xUnit functional tests for API behavior.

## Build, Test, and Development Commands
- `dotnet restore` — restore solution dependencies.
- `dotnet build Task_Management_BE.sln` — build all projects; warnings are treated as errors.
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
- Keep feature code grouped by capability, for example `Features/Tasks/Command/CreateTask`.

## Testing Guidelines
Tests use xUnit, FluentAssertions, and `Microsoft.AspNetCore.Mvc.Testing`; some scenarios rely on Testcontainers for PostgreSQL.
- Add or update functional tests for any endpoint, validation, or persistence change.
- Keep test names behavior-focused, e.g. `ShouldCreateTask` and `ShouldRequireMinimumFields`.
- No enforced coverage threshold is configured; cover new behavior rather than chasing arbitrary percentages.

## Commit & Pull Request Guidelines
Recent history follows Conventional Commit prefixes such as `feat:`, `refactor:`, and `test:`. Keep commits focused and descriptive.

For pull requests, include:
- a short summary of the change and affected layers,
- linked issue or task reference,
- test evidence (`dotnet test`, targeted test command, or API verification),
- notes for config, migrations, or new secrets when applicable.

## Security & Configuration Tips
Do not commit secrets. Use `dotnet user-secrets` for local development and keep `.env` values out of source control. When changing data models, create EF Core migrations with `dotnet ef migrations add <MigrationName> --project src/Infrastructure --startup-project src/Web`.

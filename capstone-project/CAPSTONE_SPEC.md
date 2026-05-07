# Capstone Spec - Task Management Backend API

## Problem Statement

Teams need a reliable way to create, assign, track, and report on work without losing updates when multiple users act on the same task at the same time. This project solves that problem for small teams and operational users by providing a secure ASP.NET Core backend for task management, assignment, status updates, notifications, and task reporting. The system emphasizes correctness under concurrent assignment, searchable task lists, real-time user updates, and production-ready infrastructure concerns such as health checks, caching, background processing, and container deployment.

## What Success Looks Like (Acceptance Criteria)

- [ ] A user can authenticate through the social login endpoint and receive an access token plus refresh token.
- [ ] An authenticated user can create a task with a title and optional description, and the API persists it with the current user as creator.
- [ ] An authenticated user can list tasks with pagination and filter by status, assignee, and search term.
- [ ] An authenticated user can search tasks through `GET /api/tasks/search?q=<term>` and receives a paginated result set.
- [ ] A task can be assigned to a user through `POST /api/tasks/{taskId}/assign` when the submitted `RowVersion` is current.
- [ ] Concurrent assignment attempts using a stale row version return `409 Conflict` instead of silently overwriting a previous assignment.
- [ ] Only the assigned user can update a task's status.
- [ ] Task creation, assignment, status changes, and report completion create notifications for the relevant user.
- [ ] Users can retrieve notifications with pagination and mark all notifications as read.
- [ ] A user can request a task report, and a background worker processes the report job asynchronously.
- [ ] The API exposes readiness and liveness health endpoints that report database, Redis, and RabbitMQ health when those dependencies are enabled.
- [ ] Functional tests cover the main endpoint behaviors: authentication validation, task creation, assignment, stale assignment conflict, status update authorization, search validation, and task pagination.
- [ ] The application can run locally with Docker Compose using PostgreSQL, Redis, and RabbitMQ.

## Architecture Sketch

- **Domain layer (`src/Domain`)**: contains core entities such as `Task`, `DomainUser`, `Notification`, and `ReportJob`, plus enums, constants, and domain events.
- **Application layer (`src/Application`)**: contains MediatR commands and queries, validators, DTOs, mappings, interfaces, caching keys, and business workflows for tasks, auth, users, notifications, and reports.
- **Infrastructure layer (`src/Infrastructure`)**: implements EF Core persistence with PostgreSQL, ASP.NET Identity, JWT token generation, Redis caching, SignalR notification publishing, RabbitMQ messaging, report generation, and background workers.
- **Web layer (`src/Web`)**: exposes minimal API endpoint groups for auth, tasks, users, notifications, reports, internal worker notification callbacks, and health checks.
- **Notification worker (`src/NotificationWorker`)**: optional worker process that consumes RabbitMQ notification events and calls the API's internal notification endpoint.
- **Functional tests (`tests/Application.FunctionalTests`)**: verify API behavior through `WebApplicationFactory`, xUnit, FluentAssertions, and Testcontainers for PostgreSQL.

## Tech Stack

- **Language/runtime**: C# on .NET 10.
- **Web framework**: ASP.NET Core minimal APIs.
- **Application patterns**: Clean architecture layering, CQRS-style commands and queries with MediatR, FluentValidation, AutoMapper.
- **Persistence**: Entity Framework Core with PostgreSQL and Npgsql.
- **Authentication/authorization**: ASP.NET Core Identity, JWT bearer tokens, refresh tokens, role/policy support.
- **Caching**: in-memory cache plus Redis through StackExchange.Redis.
- **Search**: PostgreSQL full-text search using `tsvector` and GIN indexing.
- **Concurrency control**: PostgreSQL `xmin` row version mapped through EF Core optimistic concurrency.
- **Messaging and real-time updates**: SignalR for live notifications; RabbitMQ for optional asynchronous notification dispatch.
- **Background processing**: hosted service for report jobs and optional notification worker service.
- **Reporting**: QuestPDF for task report generation.
- **API documentation and validation**: NSwag/OpenAPI, FluentValidation endpoint auto-validation.
- **Testing**: xUnit, FluentAssertions, `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers for PostgreSQL.
- **Deployment/runtime**: Docker, Docker Compose, Heroku container deployment support, health checks for production readiness.

## Task List (In Order)

1. [x] Set up layered .NET solution structure with Domain, Application, Infrastructure, Web, NotificationWorker, and functional test projects.
2. [x] Define domain entities for tasks, users, notifications, and report jobs.
3. [x] Configure EF Core DbContext, entity configurations, PostgreSQL migrations, and database initialization.
4. [x] Implement social login and refresh-token endpoints.
5. [x] Add JWT bearer authentication and current-user resolution.
6. [x] Implement task creation with validation and domain event dispatch.
7. [x] Implement paginated task listing with status, assignee, and search filters.
8. [x] Add PostgreSQL full-text search and `/api/tasks/search` endpoint.
9. [x] Implement task assignment.
10. [x] Add row-version based optimistic concurrency for task assignment.
11. [x] Return `409 Conflict` for stale task assignment attempts.
12. [x] Implement status update authorization so only the assigned user can update task status.
13. [x] Implement notification persistence and SignalR publishing.
14. [x] Add optional RabbitMQ notification dispatch and notification worker callback endpoint.
15. [x] Add user notification listing and mark-all-as-read behavior.
16. [x] Add task report request flow and background report processing.
17. [x] Add Redis-backed task list caching and cache invalidation on task mutations.
18. [x] Add health checks for API readiness and liveness.
19. [x] Add Docker Compose configuration for local development and production-like runs.
20. [x] Add functional tests for key API behavior.
21. [ ] Add a documented load-test script for the assignment endpoint to measure throughput and conflict behavior under concurrent requests.
22. [ ] Review production seeding and deployment documentation for consistency with current behavior.
23. [ ] Run the full functional test suite and fix any remaining failures before final capstone submission.
24. [ ] Prepare final demo notes showing authentication, task creation, assignment conflict handling, notifications, and reporting.

## Out of Scope (MVP)

- A frontend web or mobile client.
- Complex project/team hierarchy beyond users and tasks.
- File uploads for task attachments.
- Fine-grained task permissions beyond creator, assignee, authenticated user, and existing role policy support.
- Kanban boards, sprint planning, calendars, or recurring tasks.
- Multi-tenant organization isolation.
- Advanced notification preferences or email/push notification delivery.
- Payment, billing, or subscription features.
- Production-grade observability dashboards, distributed tracing, and alerting.
- Horizontal scale testing beyond targeted load simulation of the assignment endpoint.

## Open Questions

- Should task assignment require that the target assignee already exists as a `DomainUser`, or should the current foreign-key behavior be enough?
- Should task creation allow clients to set priority, due date, or initial assignee, or should those remain separate workflows?
- Should status updates also use row-version concurrency checks, not just assignment?
- Should report files be stored in database, local filesystem, Cloudinary, S3-compatible storage, or another durable object store?
- Should notification dispatch default to `InProcess` for all local development unless RabbitMQ is explicitly enabled?
- Should Redis caching be optional at startup for development and tests, or should the API continue requiring a Redis connection string?
- What performance target should the assignment endpoint meet under load, for example p95 latency and expected success/conflict distribution for 1,000 concurrent requests?
- What deployment target should be presented in the final capstone demo: Docker Compose locally, Heroku, Azure, or another platform?

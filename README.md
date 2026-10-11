# JobTracker

JobTracker tracks a user's job applications and searches external jobs through Jooble.
The frontend uses React and TypeScript; the backend uses ASP.NET Core and PostgreSQL.

## Backend layers

- **JobTracker.Api** handles HTTP routes, authentication, request validation, response
  status codes, and application startup.
- **JobTracker.BL** contains one `JobApplicationService` class for application CRUD,
  filtering, ownership checks, and response mapping. It uses `AppDbContext` directly.
- **JobTracker.DAL** owns entities, `AppDbContext`, EF Core migrations, and the
  Jooble provider.

The request flow is:

```text
Applications: React -> API controller -> JobApplicationService -> AppDbContext -> PostgreSQL
Job search:   React -> API controller -> JoobleJobProvider
```

API references BL and DAL; BL references DAL. All dependency registrations are in
`Program.cs`. A service is simply a class that handles application operations;
there are no additional interfaces, repositories, or forwarding services.

## Run with Docker

Set `JOOBLE_API_KEY` in your local `.env`, then run from the repository root:

```powershell
docker compose up --build
```

- Frontend: `http://localhost:3000`
- API: `http://localhost:8080`
- PostgreSQL from the host computer: `localhost:5433`
- PostgreSQL from another Compose container: `db:5432`

The existing database volume and migration history are retained. The API applies
pending migrations on startup.

## Build and test

```powershell
dotnet build JobTracker.sln
dotnet test JobTracker.sln
```

The existing API tests cover authentication, application CRUD, and ownership protection.
The InMemory API tests do not exercise PostgreSQL-specific `ILike` search.

## Database migrations

Migrations belong to DAL; API remains the startup project and reads the connection
string from its configuration:

```powershell
dotnet ef migrations add MigrationName --project JobTracker.DAL --startup-project JobTracker.Api
dotnet ef database update --project JobTracker.DAL --startup-project JobTracker.Api
```

Configure `ConnectionStrings:DefaultConnection` through API user secrets or the
`ConnectionStrings__DefaultConnection` environment variable when running locally.
The original migration IDs are unchanged by the layer refactor.


## Public deployment checklist

Before public deployment, review [LEGAL_COMPLIANCE.md](LEGAL_COMPLIANCE.md) and configure the
public legal metadata used by the frontend. Copy `JobTracker.Web/.env.example` into the build
environment and set:

- `VITE_OPERATOR_NAME`
- `VITE_LEGAL_CONTACT_EMAIL`
- `VITE_OPERATOR_BUSINESS_DETAILS`
- `VITE_HOSTING_PROVIDER`

These `VITE_*` values are embedded in the public frontend bundle, so do not put secrets in them.
The site intentionally does not show a cookie-consent banner while it has no non-essential
tracking; reassess this if analytics, advertising, embeds or production infrastructure add
non-essential device storage.

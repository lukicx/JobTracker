# JobTracker

JobTracker tracks a user's job applications and searches external jobs through Jobs.cz and Jooble.
The frontend uses React and TypeScript; the backend uses ASP.NET Core and PostgreSQL.

## Backend layers

- **JobTracker.Api** handles HTTP routes, authentication, request validation, response
  status codes, and application startup.
- **JobTracker.BL** contains `JobApplicationService` for application CRUD,
  filtering, ownership checks, and response mapping. It uses `AppDbContext` directly.
  `JobSearchService` combines external providers and handles partial failures.
- **JobTracker.DAL** owns entities, `AppDbContext`, EF Core migrations, and the
  Jobs.cz and Jooble providers.

The request flow is:

```text
Applications: React -> API controller -> JobApplicationService -> AppDbContext -> PostgreSQL
Job search:   React -> API controller -> JobSearchService -> JobsCzJobProvider + JoobleJobProvider
```

API references BL and DAL; BL references DAL. All dependency registrations are in
`Program.cs`. A service is simply a class that handles application operations;
there are no additional interfaces, repositories, or forwarding services.

## Run with Docker

Set `JOOBLE_API_KEY` in your local `.env` to include Jooble results, then run from the repository root.
Jobs.cz works without an API key; a missing Jooble key produces a partial-results warning.

```powershell
docker compose up --build
```

- Frontend: `http://localhost:3000`
- API: `http://localhost:8080`
- PostgreSQL from the host computer: `localhost:5433`
- PostgreSQL from another Compose container: `db:5432`

The existing database volume and migration history are retained. The API applies
pending migrations on startup.

## Jobs.cz search

Use the existing search fields, for example `C#`, `.NET`, or `junior` with `Brno`.
The same API route searches both sources by default. Set `source=jobscz` or
`source=jooble` to request only that provider:

```text
GET /api/jobs?search=C%23&location=Brno&source=jobscz
```

`JobsCzJobProvider` uses `HttpClient` and HtmlAgilityPack to parse the public search HTML.
The classic and beta layouts both return job cards without JavaScript rendering.
City names become Jobs.cz URL slugs (for example `Brno`, `Praha`, or `České Budějovice`);
the search requests a 0 km radius. Use Czech city names rather than addresses or English names.
An unrecognized location or changed page layout reports that source as unavailable.

Search starts with the first provider results page (up to 30 offers per provider).
The frontend shows up to 30 jobs per numbered page, with Previous/Next and page
buttons above and below the cards. New pages fetch provider batches as needed;
visited pages are kept in memory and repeated entries are removed by `externalId`.
Page numbers expand as results become available; they do not imply a known total.
Navigation keeps the submitted keyword, location, and source even if you edit the
form. Press Search to apply those edits and reset to page 1. Changing pages scrolls
to the results. Job links open in a new tab so the search page and scroll stay available.
It maps title, employer, location, direct link, and available card highlights to `ExternalJob`.
`Description` contains card highlights, when present; it does not fetch the full job description.
Jobs.cz keyword matching can include description text, so a title need not contain the keyword.
`junior` alone also matches roles outside software development.

IDs use `jobscz:` and `jooble:` prefixes so React card keys cannot collide between sources.
Search cards show their source. Choose All sources, Jobs.cz, or Jooble, then press Search.
The API calls only the selected provider (or both for All sources). Changing the selector
alone does not fetch or filter results; the existing list keeps the source label from its search.
Unsupported source values return HTTP 400.
The combined list keeps each source's ordering and may include the same vacancy from both sites.
When searching both sources, if one fails the API returns the other source's results and
an `X-Job-Search-Warning` header, which the frontend displays. If every requested provider
fails (including a single selected provider), the API returns HTTP 502.

### Pagination

```text
GET /api/jobs?search=junior&location=Brno&source=jobscz&page=2
```

The response is an object containing `jobs`, `nextJobsCzPage`, and `nextJooblePage`.
A null next page means that source has finished or was not selected.
Jobs.cz uses the page's next-page link; Jooble requests 30 items per page and uses `totalCount`.
Both providers are tracked separately, so a finished source is not fetched again.

For All sources, the frontend passes the returned page numbers as `jobsCzPage`
and `jooblePage`, with 0 to skip a finished provider. Those values override the common
`page` parameter, which defaults to 1. Invalid page values or skipping every selected
provider returns HTTP 400. A failed provider keeps its requested page for retry;
successful batches are retained, and a total failure preserves the current page.

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

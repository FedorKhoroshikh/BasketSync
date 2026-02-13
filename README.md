# BasketSync

Web-application for collaborative shopping lists with real-time synchronization, discount card management, and Google OAuth support.

## Tech Stack

- **Backend:** ASP.NET Core 8 Web API
- **Frontend:** Vanilla HTML / CSS / JavaScript (served as static files)
- **Database:** PostgreSQL (Npgsql)
- **Authentication:** JWT Bearer tokens + Google OAuth (GIS)
- **Architecture:** Clean Architecture + CQRS (MediatR) + AutoMapper
- **Testing:** NUnit + EF Core InMemory
- **CI/CD:** GitHub Actions + Render (Docker)

## Architecture

```
Controllers (BasketSync.WebApi)
    -> Application (Commands / Queries / Handlers, DTOs, AutoMapper)
        -> Domain (Entities, no dependencies)
        <- Infrastructure (EF Core + PostgreSQL, Repositories, UoW, Services)
```

| Layer | Project | Description |
|-------|---------|-------------|
| API | `BasketSync.WebApi` | Controllers, Program.cs, wwwroot/ (frontend) |
| Application | `Application` | CQRS commands/queries, handlers, DTOs, mapping |
| Domain | `Domain` | Pure entities: User, ShoppingList, Item, ListItem, Category, Unit, DiscountCard |
| Infrastructure | `Infrastructure` | EF Core DbContext, repositories, JWT/password/Google services |
| Tests | `BasketSyncTests` | NUnit handler tests with InMemory DB |

## Project Structure

```
BasketSync/
├── .github/workflows/     # CI + deploy workflows
├── src/
│   ├── BasketSync.sln
│   ├── BasketSync/        # WebApi project
│   │   ├── Controllers/
│   │   ├── wwwroot/       # Frontend (HTML/CSS/JS)
│   │   └── Program.cs
│   ├── Application/       # Commands, Queries, Handlers, DTOs
│   ├── Domain/            # Entities
│   ├── Infrastructure/    # EF Core, Services, Migrations
│   ├── BasketSyncTests/   # NUnit tests
│   └── Worker/            # Background jobs (placeholder)
├── Dockerfile
└── README.md
```

## API Endpoints

### Auth (anonymous)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login with name + password |
| POST | `/api/auth/google` | Google OAuth login |

### Users

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/me` | Current user profile |
| PUT | `/api/users/me/name` | Update name |
| PUT | `/api/users/me/email` | Update email |
| PUT | `/api/users/me/password` | Change password |
| GET | `/api/users/me/lists` | Shopping lists for current user |
| GET | `/api/users/{userId}/lists` | Shopping lists for specific user |
| GET | `/api/users` | All users (for share picker) |

### Shopping Lists

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/lists` | Create list |
| GET | `/api/lists/{id}` | Get list with items |
| PUT | `/api/lists/{id}` | Rename list / update sharing |
| DELETE | `/api/lists/{id}` | Delete list |
| GET | `/api/lists/{id}/shares` | Get shared user IDs |
| PUT | `/api/lists/{id}/shares` | Update sharing |
| POST | `/api/lists/{listId}/items` | Add item to list |
| DELETE | `/api/lists/{listId}/items` | Remove item from list |
| PATCH | `/api/lists/{listId}/items/{itemId}` | Update item (qty, comment, category, unit) |

### Items & Catalog

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/items?search=` | Search items by name |
| POST | `/api/items` | Create new item |
| PATCH | `/api/items/{id}/toggle` | Toggle item checked |
| GET | `/api/categories` | All categories |
| GET | `/api/categories/{id}` | Get category |
| POST | `/api/categories` | Create category |
| PUT | `/api/categories/{id}` | Update category |
| DELETE | `/api/categories/{id}` | Delete category |
| GET | `/api/units` | All units |
| POST | `/api/units` | Create unit |

### Discount Cards

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/me/cards` | Current user's cards |
| GET | `/api/cards/{id}` | Get card |
| POST | `/api/cards` | Create card |
| PUT | `/api/cards/{id}` | Update card |
| DELETE | `/api/cards/{id}` | Delete card |
| PATCH | `/api/cards/{id}/toggle` | Toggle card active |
| POST | `/api/cards/{id}/identifiers` | Add card identifier |
| PUT | `/api/identifiers/{id}` | Update identifier |
| DELETE | `/api/identifiers/{id}` | Delete identifier |
| POST | `/api/cards/resolve` | Resolve card by barcode |

### Health

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/db-check` | Database connectivity check |

## Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (localhost:5432)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`)

### Setup

1. Create a PostgreSQL database named `BasketSync`.

2. Check connection string in `src/BasketSync/appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=BasketSync;Username=postgres;Password=YOUR_PASSWORD"
   }
   ```

3. Apply migrations:
   ```bash
   dotnet ef database update --project src/Infrastructure --startup-project src/BasketSync
   ```

4. Run the application:
   ```bash
   dotnet run --project src/BasketSync/BasketSync.WebApi.csproj
   ```

5. Open `http://localhost:8080` in your browser.

### Build & Test

```bash
# Build
dotnet build src/BasketSync.sln

# Run all tests
dotnet test src/BasketSync.sln

# Run a specific test
dotnet test src/BasketSync.sln --filter "FullyQualifiedName~BasketSyncHandlersTests.CreateList_ReturnDto_Test"
```

## Deployment (Render + Neon)

The application is deployed as a Docker container on [Render](https://render.com/) with a managed PostgreSQL database on [Neon](https://neon.tech/).

### 1. Neon Database

1. Create a project at [neon.tech](https://neon.tech/).
2. Copy the connection string (includes `?sslmode=require`).
3. Apply migrations from your local machine:
   ```bash
   dotnet ef database update ^
     --project src/Infrastructure ^
     --startup-project src/BasketSync ^
     --connection "Host=ep-xxx.neon.tech;Port=5432;Database=basketsync;Username=user;Password=pass;SSL Mode=Require"
   ```

### 2. Render Web Service

1. Create a **Web Service** on Render, select **Docker**, connect your GitHub repo.
2. Set the following **Environment Variables** in Render:

   | Variable | Value |
   |----------|-------|
   | `ConnectionStrings__DefaultConnection` | Neon connection string |
   | `Jwt__Key` | Secure random string (32+ characters) |
   | `Google__ClientId` | Google OAuth Client ID |

   > `PORT` is set automatically by Render.

### 3. Google OAuth

In [Google Cloud Console](https://console.cloud.google.com/):
- Add `https://<your-app>.onrender.com` to **Authorized JavaScript origins**
- Update redirect URIs if needed

### 4. GitHub Actions (CI/CD)

CI runs automatically on push to `main` and on pull requests (build + test).

Deploy workflow triggers on push to `main`:
1. Runs CI (build + test).
2. Calls Render Deploy Hook to trigger a new deployment.

**Setup:**
1. In Render Dashboard: **Settings** -> **Deploy Hook** -> copy the URL.
2. In GitHub repo: **Settings** -> **Secrets and variables** -> **Actions** -> add secret `RENDER_DEPLOY_HOOK` with the hook URL.

### 5. Verify Deployment

- Health check: `https://<your-app>.onrender.com/db-check`
- Logs: Render Dashboard -> **Logs**

### Environment Variables Reference

| Variable | Where | Description |
|----------|-------|-------------|
| `ConnectionStrings__DefaultConnection` | Render | Neon PostgreSQL connection string |
| `Jwt__Key` | Render | JWT signing key (32+ chars) |
| `Google__ClientId` | Render | Google OAuth Client ID |
| `PORT` | Render (auto) | HTTP port (set by Render) |
| `RENDER_DEPLOY_HOOK` | GitHub Secrets | Render deploy hook URL |

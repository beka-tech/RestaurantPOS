# Restaurant POS

Restaurant POS is an in-development point-of-sale application with an Angular frontend, an ASP.NET Core backend, and a PostgreSQL database.

## Technology stack

- Frontend: Angular 22 and TypeScript
- Backend: ASP.NET Core on .NET 10
- Database: PostgreSQL 17
- Data access: Entity Framework Core 10 with Npgsql
- Local database hosting: Docker Compose

## Current project status

The frontend and backend development servers start successfully, and the initial database migration is included. The application is still under development: the API does not yet contain controllers, and the frontend routes and API services are currently placeholders.

## Prerequisites

Install the following tools before starting:

- [Git](https://git-scm.com/downloads)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) `^22.22.3`, `^24.15.0`, or `>=26.0.0`
- npm 11 (the project records npm `11.15.0`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with Docker Compose

Check the installed versions:

```bash
git --version
dotnet --version
node --version
npm --version
docker --version
docker compose version
```

## Quick start

Run all commands from the repository root unless a step says otherwise.

### 1. Start PostgreSQL

Make sure Docker Desktop is running, then start the database container:

```bash
docker compose up -d postgres
docker compose ps
```

The `restaurantpos-postgres` service should become `healthy` after a few seconds.

The development database uses:

| Setting | Value |
| --- | --- |
| Host | `localhost` |
| Host port | `5434` |
| Database | `restaurantpos_db` |
| Username | `restaurantpos` |
| Password | `restaurantpos_dev` |

These credentials are for local development only.

### 2. Create the database tables

Install the Entity Framework CLI once:

```bash
dotnet tool install --global dotnet-ef --version 10.0.12
```

If it is already installed, update it instead:

```bash
dotnet tool update --global dotnet-ef --version 10.0.12
```

Apply the included migrations:

```bash
dotnet ef database update --context RestaurantPosDbContext --project backend/src/RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj --startup-project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj
```

Run this command again whenever new migrations are added.

### 3. Start the backend

Open a new terminal in the repository root:

```bash
dotnet restore backend/src/RestaurantPOS.sln
dotnet run --project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj --launch-profile http
```

The backend runs at:

- API base URL: `http://localhost:5257`
- OpenAPI document: `http://localhost:5257/openapi/v1.json`

Keep this terminal open while developing.

### 4. Start the frontend

Open another terminal:

```bash
cd frontend/restaurant-pos
npm ci
npm start
```

Open `http://localhost:4200` in a browser. Keep this terminal open while developing.

After the first setup, the normal startup sequence is:

1. `docker compose up -d postgres`
2. Start the backend with `dotnet run`
3. Start the frontend with `npm start`

## PostgreSQL commands

Check the database status and logs:

```bash
docker compose ps
docker compose logs postgres
```

Open a PostgreSQL shell inside the container:

```bash
docker exec -it restaurantpos-postgres psql -U restaurantpos -d restaurantpos_db
```

Useful commands inside `psql`:

```text
\dt
\d "MenuItems"
\q
```

Stop PostgreSQL without deleting its data:

```bash
docker compose stop postgres
```

Stop and remove the container while keeping the database volume:

```bash
docker compose down
```

Reset the local database completely:

```bash
docker compose down -v
docker compose up -d postgres
dotnet ef database update --context RestaurantPosDbContext --project backend/src/RestaurantPOS.Infrastructure/RestaurantPOS.Infrastructure.csproj --startup-project backend/src/RestaurantPOS.Api/RestaurantPOS.Api.csproj
```

> **Warning:** `docker compose down -v` permanently deletes all data in the local development database.

## Use PostgreSQL without Docker

Docker Compose is the recommended setup. To use an existing local PostgreSQL installation instead, create the user and database as a PostgreSQL administrator:

```sql
CREATE USER restaurantpos WITH PASSWORD 'restaurantpos_dev';
CREATE DATABASE restaurantpos_db OWNER restaurantpos;
```

The application is configured for Docker's host port `5434`. A normal local PostgreSQL installation usually uses port `5432`, so override the connection string before running migrations or starting the backend.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=restaurantpos_db;Username=restaurantpos;Password=restaurantpos_dev"
```

Bash:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=restaurantpos_db;Username=restaurantpos;Password=restaurantpos_dev'
```

Run the migration and backend commands in the same terminal in which the environment variable was set.

## Build and test

Backend:

```bash
dotnet build backend/src/RestaurantPOS.sln
dotnet test backend/src/RestaurantPOS.sln
```

Frontend:

```bash
cd frontend/restaurant-pos
npm run build
npm test -- --watch=false
```

## Project structure

```text
restaurant-pos/
|-- backend/
|   `-- src/
|       |-- RestaurantPOS.Api/
|       |-- RestaurantPOS.Application/
|       |-- RestaurantPOS.Domain/
|       |-- RestaurantPOS.Infrastructure/
|       `-- RestaurantPOS.Tests/
|-- frontend/
|   `-- restaurant-pos/
|-- docker-compose.yml
`-- Readme.md
```

## Troubleshooting

### PostgreSQL does not become healthy

Inspect its logs:

```bash
docker compose logs postgres
```

Also confirm that no other program is using port `5434`.

### Password changes in `docker-compose.yml` have no effect

PostgreSQL applies its initialization values only when the data volume is first created. If local data can be discarded, reset the database with `docker compose down -v`, then start it again.

### `dotnet ef` is not recognized

Close and reopen the terminal after installing the tool, then check:

```bash
dotnet ef --version
```

### The backend cannot connect to PostgreSQL

Confirm that the container is healthy and that the connection string matches `backend/src/RestaurantPOS.Api/appsettings.Development.json`. The configured Docker port is `5434`, not PostgreSQL's usual host port `5432`.

### `npm ci` reports an unsupported Node version

Upgrade Node.js to a version listed in the prerequisites, delete no lock files, and run `npm ci` again.

## Build the Application layer yourself

Follow the [MediatR + CQRS hands-on guide](docs/APPLICATION_LAYER_GUIDE.md) to build commands, queries, validators, handlers, and tests using the existing domain entities. It provides exercises and checkpoints so you can implement the code yourself.

Continue with the [ASP.NET Core Identity, RBAC, and ABAC guide](docs/AUTHENTICATION_AUTHORIZATION_GUIDE.md) to build employee sign-in, role policies, and resource permissions yourself.

## Git workflow

See [RestaurantPOS-Git-Workflow.md](RestaurantPOS-Git-Workflow.md) for branch naming and contribution guidance.

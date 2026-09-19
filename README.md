# CoreUp Backend MVP

Backend API for the CoreUp mobile workout application. It uses .NET 10, ASP.NET Core, EF Core and PostgreSQL 17.

## Prerequisites

- .NET SDK 10
- Docker Desktop

## Run locally

Copy `.env.example` to `.env` and choose a local PostgreSQL password, then run:

```bash
docker compose up -d
dotnet tool restore
dotnet restore CoreUp.sln
dotnet tool run dotnet-ef database update --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api
dotnet run --project src/CoreUp.Api --launch-profile http
```

Development URLs:

- API: `http://localhost:5027`
- Swagger UI: `http://localhost:5027/swagger`
- Health: `http://localhost:5027/health`

Register or log in through Swagger, copy the returned `accessToken`, then use the **Authorize** button. Exercise master data is created by the `InitialMvp` migration.

## Verify

```bash
dotnet build CoreUp.sln
dotnet test CoreUp.sln
docker compose ps
```

# Booking App

A full-stack workspace booking application where employees can reserve desks, office spaces, or parking spots. Built with **ASP.NET Core 9** (backend) and **Angular 19** (frontend), following Clean Architecture / DDD principles.

## Data Structure

![Data Structure](data-structure.png)

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Backend — Applied Improvements](#backend--applied-improvements)
- [Frontend — Applied Improvements](#frontend--applied-improvements)
- [Known Technical Debt](#known-technical-debt)

---

## Architecture Overview

```
booking-app/
├── backend/        # ASP.NET Core 9 Web API
│   └── src/
│       ├── booking.api            # Controllers, Program.cs, config
│       ├── booking.core           # Domain: interfaces, DTOs, models, constants
│       └── booking.infrastructure # Persistence, services, EF Core, SQLite
└── frontend/       # Angular 19 SPA
    └── src/app/
        ├── core/          # Guards, interceptors, global state (AuthStore)
        ├── features/
        │   ├── auth/      # Login feature (domain / data / presentation)
        │   └── booking/   # Booking feature (domain / data / presentation)
        └── shared/        # Shared utilities (Mapper base class)
```

Both layers follow **Clean Architecture**:
- `domain/` — models, abstract repositories, use cases (no framework dependencies)
- `data/` — concrete repository implementations, HTTP clients, entity mappers
- `presentation/` — components, pages, stores

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend framework | ASP.NET Core 9 |
| ORM | Entity Framework Core 9 + SQLite |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Validation | FluentValidation |
| Frontend framework | Angular 19 (standalone components) |
| UI library | Angular Material 19 |
| State management | RxJS BehaviorSubject + Angular Signals (hybrid) |
| HTTP | Angular HttpClient + functional interceptor |
| Build tool | Angular CLI 19 / Vite |

---

## Project Structure

### Backend

```
booking.core/
├── Constants/     Codes.cs — response code constants (Ok, NotFound, InvalidInput…)
├── DTOs/          Request/response DTOs (BookingUpsertDto, Availability, etc.)
├── Interfaces/    IAuthService, IBookingService, IOfficeService, IServicesService
└── Models/        Domain models (BookingModel, OfficeModel, ServiceModel…)

booking.infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── DatabaseBootstrapper.cs      Schema init + seed (demo data only in Development)
│   ├── DbInitializerHostedService.cs
│   └── Sql/schema.sql               DDL for all tables
└── Services/
    ├── AuthService.cs
    ├── BookingService.cs
    ├── OfficeService.cs
    └── ServiceService.cs

booking.api/
├── Controllers/   AuthController, BookingController, OfficeController, ServicesController
├── Program.cs     DI registration, JWT, CORS, authorization policies
├── appsettings.json
└── appsettings.Development.json
```

### Database

| Property | Value |
|----------|-------|
| Engine | SQLite 3 (file-based, no server required) |
| File | `app.db` (created at runtime in the API project directory) |
| Connection string | `Data Source=app.db` |

Tables: `Users`, `Roles`, `UserRoles`, `UserClaims`, `RoleClaims`, `Employees`, `Office`, `Service`, `Service_Details`, `Booking`

### Frontend

```
core/
├── guards/            AuthGuard (protect booking routes), GuestGuard (redirect if already logged in)
├── interceptors/      auth.interceptor.ts — attaches Bearer token, handles 401
└── state/             AuthStore — session state (BehaviorSubject + localStorage)

features/auth/
├── domain/            AuthRepository (abstract), LoginUseCase, LogoutUseCase, GetCurrentUserUseCase
├── data/              AuthImplRepository (HTTP)
└── presentation/      LoginComponent

features/booking/
├── domain/            OfficeRepository, ServiceRepository, BookingRepository + use cases
├── data/              Impl repositories, entity mappers
└── presentation/
    ├── state/         BookingStore
    ├── pages/         WorkingPlaceComponent, ServiceComponent, ServiceDetailComponent
    └── components/    WorkingPlaceCardComponent, ServiceCardComponent,
                       ServiceDetailCardComponent, BookingModal
```

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) + npm
- Angular CLI: `npm install -g @angular/cli`

### Backend

```powershell
cd backend

# Set the JWT signing key via user-secrets (required — never commit this value)
dotnet user-secrets set "Jwt:Key" "YourStrongSecretKeyHere123!@#" --project src/booking.api/booking.api.csproj

# Run in Development (required to seed demo data)
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/booking.api/booking.api.csproj
```

API runs at `https://localhost:7251` / `http://localhost:5170`  
Swagger UI: `http://localhost:5170/swagger`

#### Demo credentials (seeded in Development only)

| Field | Value |
|-------|-------|
| Email | `admin@demo.com` |
| Password | `P@ssw0rd!` |

### Frontend

```bash
cd frontend
npm install
ng serve
```

App runs at `http://localhost:4200`

> The development environment file (`environment.development.ts`) already points to `http://localhost:5170/api`. No changes needed for local development.

---

## Backend — Applied Improvements

### 1. Dependency Inversion (Architecture)

**Problem:** Service interfaces (`IAuthService`, `IBookingService`, `IOfficeService`, `IServicesService`) were defined inside `booking.infrastructure` alongside their implementations, forcing `booking.api` controllers to depend directly on Infrastructure.

**Fix:** All 4 interfaces moved to `booking.core/Interfaces/`. Infrastructure classes now only implement them. Controllers reference `booking.core.Interfaces` for contract types.

---

### 2. Missing Authorization on Endpoints

**Problem:** `Program.cs` defined detailed permission policies (`Booking.Read/Create/Update/Delete`, `Office.*`, `Service.*`) but no controller had `[Authorize]`. Any unauthenticated caller could create, edit, or delete records.

**Fix:** `[Authorize]` added at controller level and `[Authorize(Policy = "...")]` per action on `BookingController`, `OfficeController`, and `ServicesController`. `AuthController` (login/register) remains public.

---

### 3. Mass Assignment on Resource Creation

**Problem:** `BookingUpsertDto`/`OfficeUpsertDto` included `id`, and services assigned it directly on create — allowing the client to choose the ID of new records. `BookingService` also accepted `createdAt` from the client.

**Fix:** `BookingService.CreateAsync` and `OfficeService.CreateAsync` no longer assign `Id` from the DTO (the database generates it). `CreatedAt` is now set server-side (`DateTime.UtcNow`) on both create and update.

---

### 4. Secrets in the Repository

**Problem 1:** `appsettings.json` had the JWT signing key (`Jwt:Key`) in plain text committed to the repo.

**Fix:** Value removed from `appsettings.json`. A new key is stored via `dotnet user-secrets` (outside the repo). The existing null-check in `Program.cs` now acts as a hard guardrail — startup fails with a clear message if the key is missing.

> **The previous key must be considered compromised.** If it was used in any real environment, rotate it there as well.

**Problem 2:** The demo user password (`P@ssw0rd!`) was hardcoded in `DatabaseBootstrapper.cs`, and demo seeding ran in every environment including production.

**Fix:**
- Password moved to config: `Seed:DemoUserPassword` in `appsettings.Development.json`.
- Seed split into two phases: `SeedRolesAndPermissionsAsync` (always runs — required for auth policies) and `SeedDemoDataAsync` (**Development only**).
- The `r_admin` role now includes `Office.*` and `Service.*` claims in addition to `Booking.*`.

---

### 5. Unhandled Exception in Availability Endpoint

**Problem:** `BookingService.GetAvailability` used `DateTime.Parse`/`int.Parse` without error handling or invariant culture. A malformed date or frequency caused an unhandled 500.

**Fix:** Replaced with `TryParse` using `CultureInfo.InvariantCulture`, with validation that frequency is a positive number. Invalid input returns `ok = false` with a message; the controller responds `400 Bad Request` (previously returned `404`).

---

### 6. Configurable CORS

**Problem:** The allowed origin (`http://localhost:4200`) was hardcoded in `Program.cs`.

**Fix:** Now read from `Cors:AllowedOrigins` in configuration, with `http://localhost:4200` as fallback. For production, add the section to `appsettings.Production.json`.

---

### 7. Dead Code Removal

- Removed `DbInitializer.cs` — never invoked, created two inconsistent DB initialization paths.
- Removed `Class1.cs` in `booking.core` and `booking.infrastructure` — unused `dotnet new classlib` scaffolding.
- Removed duplicated email/password validation in `AuthService.RegisterAsync` — already covered by `RegisterRequestValidator` (FluentValidation).
- Renamed `Controllers/ServiceController.cs` → `Controllers/ServicesController.cs` to match the class name (HTTP route `api/Services` unchanged).

---

## Frontend — Applied Improvements

### 1. JWT Token Not Sent in HTTP Requests

**Problem:** The JWT token was stored in `AuthStore`/`localStorage` after login, but no `HttpInterceptor` attached it to outgoing requests. All Booking/Office/Services calls failed with 401 after backend authorization was added.

**Fix:** New functional interceptor `core/interceptors/auth.interceptor.ts` — adds `Authorization: Bearer <token>` when a session is active. If the backend returns 401 on a request that carried a token (expired/invalid session), it clears the session and redirects to `/auth/login`. Does not trigger logout on a failed login attempt (no token was sent in that case).

---

### 2. Unprotected Routes

**Problem:** `AuthGuard` was imported in `app.routes.ts` but the block applying it was commented out. `RoleGuard` was not referenced in any route. Anyone could navigate to `/booking/*` without logging in.

**Fix:** `canActivate: [AuthGuard]` applied to the `booking` route. Dead commented block removed.

---

### 3. Session Lost on Page Refresh

**Problem:** `AuthStore.loadSession()` (restores session from `localStorage`) was defined but never called.

**Fix:** Registered as `provideAppInitializer(() => inject(AuthStore).loadSession())` in `app.config.ts`, running before the router resolves the initial navigation.

---

### 4. Hardcoded API URL

**Problem:** `'http://localhost:5170/api'` was repeated verbatim in 4 repository files. No way to switch environments without editing source code.

**Fix:**
- New `src/environments/environment.ts` (production default: `apiUrl: '/api'`) and `environment.development.ts` (`apiUrl: 'http://localhost:5170/api'`).
- `angular.json` development build does `fileReplacements` for the environment file.
- All 4 repositories now read `environment.apiUrl`.

---

### 5. Office Filter Not Working in Services

**Problem:** `ServiceImplRepository.getServices(officeId)` sent `?officeId=X` to the backend, but `ServicesController.GetAll` does not read that parameter — it always returns all services from all offices.

**Fix:** Client-side filter applied in `ServiceImplRepository` by `entity.officeId === officeId` before mapping the response. This is a temporary workaround until the backend supports the filter.

---

### 6. RoleGuard Side Effect

**Problem:** `RoleGuard.canActivate()` called `router.navigate()` as a side effect and returned a synchronous `boolean`, unlike `AuthGuard` which returns `Observable<boolean | UrlTree>`.

**Fix:** `RoleGuard` now returns `boolean | UrlTree` using `createUrlTree`, consistent with `AuthGuard`.

---

### 7. Login Without Error Feedback

**Problem:** On login failure, the only trace was a `console.error`. The user saw nothing on screen.

**Fix:** `LoginComponent` has an `errorMessage` signal displayed in the template on failure. The submit button is disabled while `isLoading()` is true. Debug `console.log`/`console.error` calls removed.

---

### 8. Login Route Not Protected (Guest Guard)

**Problem:** A user already logged in could navigate back to `/auth/login` and see the login page again.

**Fix:** New `GuestGuard` (`core/guards/guest.guard.ts`) — if `isLoggedIn` is `true`, redirects to `/booking/working-place`. Applied to the `/auth/login` route.

---

### 9. Logout Button

**Fix:** `BookingComponent` now renders an Angular Material toolbar with a logout icon button visible on all booking pages. On click, calls `AuthStore.clearSession()` (clears in-memory state and `localStorage`) and redirects to `/auth/login`.

---

### 10. Dead Code Removal

- `BookingImplRepository` declared `mapper = new OfficeRepositoryMapper()` (wrong mapper, copy-paste artifact) without using it. Field and import removed.
- `ServiceImplRepository` had unused imports for `OfficeRepositoryMapper`, `ServiceDetailRepositoryMapper`, and `ServiceDetailEntity`. Removed.
- Commented "TESTING" blocks and `console.log('wPlaces', ...)` removed from `booking.store.ts`.

---

## Known Technical Debt

| Area | Issue | Priority |
|------|-------|----------|
| Backend | Service methods return heterogeneous tuples `(bool ok, T data, string code, string? message)` instead of a unified `Result<T>` type — shape is inconsistent across all 4 services and fragile to maintain | Medium |
| Backend | `ServicesController.GetAll` does not support `officeId` filtering — currently patched client-side in the frontend | Low |
| Backend | No server-side validation of booking business rules (date range, overlap, max 7 weeks, working days) — validated only on the client | High |
| Frontend | JWT stored in `localStorage` (not `HttpOnly` cookie) — known XSS trade-off, requires backend session cookie support to fix | Medium |
| Frontend | `RoleGuard` implemented but not applied to any route — no admin section exists in the UI yet | Low |
| Frontend | Each standalone component imports individual Angular Material modules — a shared `MaterialModule` would reduce repetition significantly | Low |

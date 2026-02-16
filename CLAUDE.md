# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Perfume Tracker is a full-stack web app for tracking and analyzing a perfume collection with AI-powered recommendations. Two separate projects in one repo:

- **Backend:** ASP.NET Core 10 (C#) Web API in `PerfumeTracker.Server/`
- **Frontend:** Next.js 15 (React 19, TypeScript) in `perfumetracker.client/`
- **Tests:** xUnit v3 integration tests in `PerfumeTracker.xTests/`
- **Database:** PostgreSQL 16+ with pgvector extension
- **Solution file:** `PerfumeTracker.sln`

## Build & Run Commands

### Backend (.NET)
```bash
dotnet build                                    # Build entire solution
dotnet run --project PerfumeTracker.Server      # Run API server
dotnet ef database update --project PerfumeTracker.Server  # Apply migrations
```

### Frontend (Next.js)
```bash
cd perfumetracker.client
npm install --legacy-peer-deps                  # Install deps (legacy flag required)
npm run dev                                     # Dev server with Turbopack
npm run build                                   # Production build
npm run lint                                    # ESLint
```

### Tests
```bash
# All backend tests
dotnet test PerfumeTracker.xTests

# Single test by name
dotnet test PerfumeTracker.xTests --filter "FullyQualifiedName~TestMethodName"

# Frontend tests
cd perfumetracker.client && npm test            # Vitest
```

### Docker
```bash
docker-compose up                               # Full local stack
```

## Architecture

### Backend — Vertical Slice + CQRS

The backend uses **MediatR** for CQRS and **Carter** for minimal API endpoints (no controllers). Each feature is a self-contained vertical slice under `PerfumeTracker.Server/Features/`:

```
Features/<FeatureName>/
├── <Action><Entity>Command.cs    # Command record + MediatR handler
├── <Action><Entity>Query.cs      # Query record + MediatR handler
├── <Action><Entity>Endpoint.cs   # Carter endpoint (MapGet/MapPost/etc.)
├── <Action><Entity>Validator.cs  # FluentValidation rules
└── <Entity>Service.cs            # Optional service interface + implementation
```

Key features: `Perfumes/`, `Auth/`, `ChatAgent/`, `Embedding/`, `Tags/`, `Missions/`, `Achievements/`, `Streaks/`, `PerfumeRatings/`, `Outbox/`, `R2/`

**Important patterns:**
- `ValidationBehavior` in `Behaviors/` — MediatR pipeline that auto-validates commands
- `GlobalExceptionHandler` in `Features/Common/` — centralized error handling
- Multi-tenancy via `ITenantProvider` query filters on the DbContext (soft deletes with `IsDeleted`)
- Background services for async work: embeddings, AI identification, tag backfill, outbox processing
- SignalR hubs for real-time push (missions, chat progress)
- DTOs in `DTO/`, models in `Models/`, migrations in `Migrations/`

### Frontend — Next.js App Router

- Pages under `src/app/` using App Router with `"use client"` directives for interactive components
- Reusable UI components in `src/components/ui/` (Radix UI primitives + Tailwind)
- API services in `src/services/` (Axios-based, with interceptors in `axios-service.ts`)
- Global state via Zustand in `src/stores/user-store.ts`
- SignalR connection managed in `src/app/layout.tsx`
- Type definitions in `src/dto/`

### Testing

- **Backend:** xUnit v3 with fixture-based setup (`Fixture/`), Moq for mocking, Bogus for fake data, `WebApplicationFactory` for integration tests. Tests run against PostgreSQL with pgvector. Test environment is QA (`.runsettings`).
- **Frontend:** Vitest with `@testing-library/react`, jsdom environment. Test files are `*.test.tsx` colocated with components.

## Code Style

### C# (.editorconfig enforced)
- **Tab indentation**, no newline before open braces (K&R style), no extra newlines between members
- Nullable reference types enabled
- Record types for commands/queries
- Naming: `<Action><Entity>Command/Query/Handler/Endpoint/Validator`

### TypeScript/React
- Strict mode enabled
- Functional components only
- File naming: kebab-case for files, PascalCase for components
- `@/` path alias maps to `./src/`

## CI/CD

GitHub Actions workflow builds and tests both projects, runs EF migrations against PostgreSQL 18 + pgvector, uploads coverage to Codecov, and pushes Docker images to Docker Hub.

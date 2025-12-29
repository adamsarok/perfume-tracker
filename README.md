## What is Perfume Tracker?

Perfume Tracker is a NextJS app which lets you track, review, and analyze your perfume collection with AI-powered recommendations.   

![screenshot](https://raw.githubusercontent.com/adamsarok/perfume-tracker/refs/heads/main/screenshot.jpg)
## Features

✅ Track Your Collection → Track and review your perfumes.

✅ Usage Logging → "Spray On" function lets you track usage.

✅ Full-Text Search → Find perfumes to review or check perfumes with zero stock.

✅ AI Recommendations → Get smart perfume suggestions based on your preferences.

✅ Random Picks → Get a random perfume choice, find that one bottle that you forget to wear.

## Tech Stack
- Frontend: Next.js 15
- Backend: ASP.NET Core 9
- Database: PostgreSQL
- Storage: Cloudflare R2

## Quick Start

Docker-compose:

```
services:
    perfume-tracker:
        image: adamsarok/perfume-tracker
        ports:
          - 3000:3000
        environment:
          - NEXT_PUBLIC_PERFUMETRACKER_API_ADDRESS=http://perfume-tracker-api:8080
        restart: unless-stopped

    perfume-tracker-api:
        image: adamsarok/perfume-tracker-api
        ports:
          - 7080:8080
          - 7081:8081
        environment:
          - ConnectionStrings__DefaultConnection=Server=db; Port=5432; User Id=postgres; Password=postgres; Database=perfumetracker
          - OpenAi__ApiKey=${OPENAI_API_KEY}
          - Jwt__Key=${JWT_KEY}
          - Jwt__Issuer=PerfumeTrackerServer
          - Jwt__Audience=PerfumeTrackerClient
          - Jwt__ExpirationHours=24
          - Users__AdminUserName=admin
          - Users__AdminEmail=admin@example.com
          - Users__AdminPassword=${ADMIN_PASSWORD}
          - Users__DemoUserName=demo
          - Users__DemoEmail=demo@example.com
          - Users__DemoPassword=${DEMO_PASSWORD}
          - R2__AccessKey=${R2_ACCESS_KEY}
          - R2__SecretKey=${R2_SECRET_KEY}
          - R2__AccountId=${R2_ACCOUNT_ID}
          - R2__BucketName=${R2_BUCKET_NAME}
          - R2__ExpirationHours=24
          - R2__MaxFileSizeKb=256
          - RateLimits__General=250
          - RateLimits__Auth=20
          - RateLimits__Upload=10
          - CORS__AllowedOrigins__0=${SERVER_ADDRESS}
          - OpenAI__ApiKey=${OPENAI_API_KEY}
          - OpenAI_AssistantModel=gpt-5-nano-2025-08-07

        restart: unless-stopped

    db:
      image: postgres:16.5
      restart: always
      shm_size: 128mb
      volumes:
      - ./postgres:/var/lib/postgresql/data
      environment:
        POSTGRES_PASSWORD: postgres
        PGDATA: /var/lib/postgresql/data/pgdata
        POSTGRES_DB: perfumetracker 
      ports:
        - 5432:5432
```

## Badges

![Docker Image CI](https://github.com/adamsarok/perfume-tracker/actions/workflows/docker-image.yml/badge.svg)
[![codecov](https://codecov.io/gh/adamsarok/perfume-tracker/graph/badge.svg?token=U4CVA3ZUAJ)](https://codecov.io/gh/adamsarok/perfume-tracker)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=adamsarok_perfume-tracker&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=adamsarok_perfume-tracker)

Client: [![Docker Hub](https://img.shields.io/docker/pulls/adamsarok/perfume-tracker.svg)](https://hub.docker.com/r/adamsarok/perfume-tracker)

API: [![Docker Hub](https://img.shields.io/docker/pulls/adamsarok/perfume-tracker-api.svg)](https://hub.docker.com/r/adamsarok/perfume-tracker-api)

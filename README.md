## What is Perfume Tracker?

Perfume Tracker is a NextJS app which lets you track, review, and analyze your perfume collection with AI-powered recommendations.

## Features

✅ Track Your Collection → Track and review your perfumes.

✅ Usage Logging → "Spray On" function lets you track usage.

✅ Full-Text Search → Find perfumes to review or check perfumes with zero stock.

✅ AI Recommendations → Get smart perfume suggestions based on your preferences.

✅ Random Picks → Get a random perfume choice, find that one bottle that you forget to wear.

## Tech Stack
- Frontend: Next.js 15 (Zustand, ShadCN)
- Backend: ASP.NET Core 9
- Database: PostgreSQL
- CDN/Storage: Go & Cloudflare R2

## Quick Start
Example docker-compose:

```
services:
    perfume-tracker:
        image: adamsarok/perfume-tracker
        ports:
          - 3000:3000
        environment:
          - NEXT_PUBLIC_R2_API_ADDRESS=http://localhost:9088
          - PERFUMETRACKER_API_ADDRESS=http://perfume-tracker-api:8080/api
        restart: unless-stopped

    perfume-tracker-api:
        image: adamsarok/perfume-tracker-api
        ports:
          - 7080:8080
          - 7081:8081
        environment:
          - ConnectionStrings__DefaultConnection=Server=db; Port=5432; User Id=postgres; Password=postgres; Database=perfumetracker
          - OpenAi__ApiKey=${OPENAI_API_KEY}
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

    r2-api-go:
        image: adamsarok/r2-api-go
        ports:
          - 9088:8080
        environment:
          - R2_ENDPOINT=${R2_ENDPOINT}
          - R2_BUCKET=test
          - R2_REGION=auto
          - R2_ACCESS_KEY=${R2_ACCESS_KEY}
          - R2_SECRET_KEY=${R2_SECRET_KEY}
          - R2_UPLOAD_EXPIRY_MINUTES=30m
          - R2_DOWNLOAD_EXPIRY_MINUTES=30m
          - CACHE_DIR=/config
        volumes:
          - ./r2-api-go/cache:/config
        restart: unless-stopped
```

## Badges

![Docker Image CI](https://github.com/adamsarok/perfume-tracker/actions/workflows/docker-image.yml/badge.svg)
[![codecov](https://codecov.io/gh/adamsarok/perfume-tracker/graph/badge.svg?token=U4CVA3ZUAJ)](https://codecov.io/gh/adamsarok/perfume-tracker)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=adamsarok_perfume-tracker&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=adamsarok_perfume-tracker)

Client: [![Docker Hub](https://img.shields.io/docker/pulls/adamsarok/perfume-tracker.svg)](https://hub.docker.com/r/adamsarok/perfume-tracker)

API: [![Docker Hub](https://img.shields.io/docker/pulls/adamsarok/perfume-tracker-api.svg)](https://hub.docker.com/r/adamsarok/perfume-tracker-api)

CDN: [![Docker Hub](https://img.shields.io/docker/pulls/adamsarok/r2-api-go.svg)](https://hub.docker.com/r/adamsarok/r2-api-go)

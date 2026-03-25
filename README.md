# MaalCaCMS

Umbraco 17 headless CMS for the MaalCa ecosystem.

## Stack
- Umbraco 17.2.2 / .NET 10.0
- SQL Server 2022 (Docker)
- Delivery API v2 (headless)

## Quick Start

1. Start SQL Server:
   ```bash
   docker compose up -d
   ```

2. Restore and run:
   ```bash
   dotnet restore
   dotnet run
   ```

3. Access backoffice: http://localhost:5011/umbraco

## Document Types (code-first)

**Element Types:** textBlock, imageBlock, quoteBlock, fileBlock

**Document Types:** ecosystemProject, editorialContent, epubViewer, article, book, property, doctorProfile, ciriSonicService, affiliateLanding, testimonial

## API

- Delivery API: `http://localhost:5011/umbraco/delivery/api/v2/content`
- API Key: configured in `appsettings.json`

## CORS

- `http://localhost:3000` (maalca-web dev)
- `https://maalca.com` (production)

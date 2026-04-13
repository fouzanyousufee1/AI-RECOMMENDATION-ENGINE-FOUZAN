# AI Recommendation Engine

Full-stack movie recommendation app with a React frontend and an ASP.NET Core Web API backend.

## Project Structure

- MovieApi: ASP.NET Core Web API, Entity Framework Core, PostgreSQL, pgvector package references
- movie-ui: React 19 + Vite frontend

## Stack

- Frontend: React, Vite
- Backend: ASP.NET Core (.NET 10)
- Database: PostgreSQL
- Packages configured in backend: Entity Framework Core, OpenAI SDK, pgvector

## Current App Behavior

The frontend sends a mood prompt to the backend at POST /api/recommend.

The backend converts that mood text into a 1536-number embedding with OpenAI. Each movie description is also stored as a 1536-number embedding in PostgreSQL. pgvector compares the direction of the search vector against the direction of every stored movie vector and returns the three closest matches. In plain English, the app is comparing meaning instead of just matching repeated words.

## Prerequisites

- .NET 10 SDK
- Node.js and npm
- PostgreSQL

## Local Setup

1. Create your local backend development settings:

   Copy MovieApi/appsettings.Development.example.json to MovieApi/appsettings.Development.json and fill in your own values.

2. Install frontend dependencies:

```powershell
cd movie-ui
npm install
```

## Run The App

Start the backend:

```powershell
cd MovieApi
dotnet run --urls "http://localhost:5053"
```

Seed the database in a second terminal or with an HTTP client:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5053/api/seed"
```

Start the frontend in another terminal:

```powershell
cd movie-ui
npm run dev
```

Then open the Vite URL shown in the terminal, typically:

- http://localhost:5173

## API Endpoint

- POST /api/seed
- POST /api/recommend

`POST /api/seed` embeds the fixed 30-movie list and stores it in PostgreSQL.

Example request body:

```json
{
  "mood": "funny and clever"
}
```

## Notes

- The backend CORS policy allows localhost frontend ports for local development.
- The frontend is configured to try the backend on http://localhost:5053 first, then http://localhost:5000.
- Local secret values are not committed. Use the example development settings file as the template.
- The recommendation endpoint returns the top 3 matches with title, genre, description, and a score between 0 and 1.

## Scripts

Frontend:

- npm run dev
- npm run build
- npm run preview

Backend:

- dotnet build
- dotnet run --urls "http://localhost:5053"
# AI Movie Recommendation Engine

A full-stack web application that recommends movies based on mood using AI embeddings.

## Tech Stack
- React + Vite (frontend)
- .NET 10 Web API (backend)
- PostgreSQL + pgvector (database)
- OpenAI text-embedding-3-small (AI embeddings)

## How it works

When you type a mood like "something funny and clever", the API sends that text to OpenAI and gets back 1536 numbers. That list of numbers is the embedding. Each movie description is stored the same way in the database. pgvector compares the search numbers with each movie's numbers and finds the three movie descriptions that point most closely in the same direction. That is why the app can find related movies even when the exact words do not match.

## API Endpoints

- POST /api/seed
- POST /api/recommend

`POST /api/seed` inserts the assignment's fixed list of 30 movies and stores an embedding for each description.

`POST /api/recommend` accepts `{ "mood": "..." }`, creates an embedding for the mood text, and returns the top 3 closest movies with scores.

## Running the app

Development secrets:
- Use MovieApi/appsettings.Development.example.json as a template for your local MovieApi/appsettings.Development.json.

Start the API:
cd MovieApi
dotnet run --urls "http://localhost:5053"

Seed the database once the API is running:
Invoke-RestMethod -Method Post -Uri "http://localhost:5053/api/seed"

Start the frontend:
cd movie-ui
npm run dev

Then open http://localhost:5173 in your browser.

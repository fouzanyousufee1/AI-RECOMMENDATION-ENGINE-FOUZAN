# AI Movie Recommendation Engine

A full-stack web application that recommends movies based on mood using AI embeddings.

## Tech Stack
- React + Vite (frontend)
- .NET 9 Web API (backend)
- PostgreSQL + pgvector (database)
- OpenAI text-embedding-3-small (AI embeddings)

## How it works

When you type a mood like "something funny and clever", the app sends that text to OpenAI which converts it into a list of 1536 numbers. Each number captures a tiny piece of the meaning of your words. Every movie description in the database was converted the same way when the app was seeded. PostgreSQL then compares your 1536 numbers against every movie by measuring how similar the directions of the two number lists are. Movies whose numbers point in the most similar direction to yours come back as the top matches. The closer the direction, the higher the match percentage shown on the card.

## Running the app

Development secrets:
- Use MovieApi/appsettings.Development.example.json as a template for your local MovieApi/appsettings.Development.json.

Start the API:
cd MovieApi
dotnet run --urls "http://localhost:5000"

Start the frontend:
cd movie-ui
npm run dev

Then open http://localhost:5173 in your browser.

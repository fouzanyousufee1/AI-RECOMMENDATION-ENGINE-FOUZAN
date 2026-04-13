import { useState } from "react";
import "./App.css";

function App() {
  const [mood, setMood] = useState("");
  const [movies, setMovies] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const API_BASES = [
    import.meta.env.VITE_API_BASE_URL,
    "http://localhost:5053",
    "http://localhost:5000",
  ].filter(Boolean);

  const findMovies = async () => {
    if (!mood.trim()) return;
    setLoading(true);
    setError("");
    setMovies([]);
    try {
      let data = null;
      let lastError = null;

      for (const base of API_BASES) {
        try {
          const res = await fetch(`${base}/api/recommend`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ mood }),
          });

          if (!res.ok) {
            throw new Error(`API returned ${res.status}`);
          }

          data = await res.json();
          break;
        } catch (e) {
          lastError = e;
        }
      }

      if (!data) {
        throw lastError ?? new Error("Could not reach backend API");
      }

      setMovies(data);
    } catch {
      setError("Could not reach the API. Start backend on port 5053 or 5000.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="app-shell">
      <section className="glass-panel">
        <h1>Movie Recommendation Engine</h1>
        <p className="subtitle">Describe a mood and find your next watch</p>

        <div className="search-row">
          <input
            type="text"
            placeholder="e.g. something funny with a twist ending"
            value={mood}
            onChange={(e) => setMood(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && findMovies()}
          />
          <button onClick={findMovies} disabled={loading}>
            {loading ? "Searching..." : "Find Movies"}
          </button>
        </div>

        {loading && <p className="status-msg">Finding the best matches...</p>}
        {error && <p className="error-msg">{error}</p>}

        <div className="movie-grid">
          {movies.map((movie, i) => (
            <article key={i} className="movie-card">
              <p className="movie-genre">{movie.genre}</p>
              <p className="movie-title">{movie.title}</p>
              <p className="movie-description">{movie.description}</p>
              <span className="movie-score">{Math.round(movie.score * 100)}% match</span>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

export default App;

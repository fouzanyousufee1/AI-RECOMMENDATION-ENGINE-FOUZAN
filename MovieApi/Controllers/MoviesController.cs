using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class MoviesController : ControllerBase
{
	private readonly MovieContext _context;

	public MoviesController(MovieContext context)
	{
		_context = context;
	}

	public sealed class RecommendRequest
	{
		public string Mood { get; set; } = string.Empty;
	}

	public sealed class RecommendResponse
	{
		public string Title { get; set; } = string.Empty;
		public string Genre { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public double Score { get; set; }
	}

	[HttpPost("recommend")]
	public async Task<ActionResult<List<RecommendResponse>>> Recommend([FromBody] RecommendRequest request)
	{
		if (request is null || string.IsNullOrWhiteSpace(request.Mood))
		{
			return BadRequest("Mood is required.");
		}

		var mood = request.Mood.Trim().ToLowerInvariant();

		var movies = await _context.Movies
			.AsNoTracking()
			.OrderBy(m => m.Id)
			.Take(300)
			.ToListAsync();

		var ranked = movies
			.Select(m => new RecommendResponse
			{
				Title = m.Title,
				Genre = m.Genre,
				Description = m.Description,
				Score = CalculateScore(m, mood)
			})
			.OrderByDescending(m => m.Score)
			.ThenBy(m => m.Title)
			.Take(12)
			.ToList();

		return Ok(ranked);
	}

	private static double CalculateScore(Movie movie, string mood)
	{
		var text = $"{movie.Title} {movie.Genre} {movie.Description}".ToLowerInvariant();

		var terms = mood.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (terms.Length == 0)
		{
			return 0;
		}

		var matches = terms.Count(text.Contains);
		var score = (double)matches / terms.Length;

		return Math.Clamp(score, 0.05, 0.99);
	}
}

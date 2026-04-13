using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenAI;
using OpenAI.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace MovieApi.Controllers;

[ApiController]
[Route("api")]
public class MoviesController : ControllerBase
{
	private const string EmbeddingModel = "text-embedding-3-small";

	private static readonly IReadOnlyList<SeedMovie> SeedMovies =
	[
		new("The Dark Knight", "Action / Thriller", "A masked vigilante fights a chaotic criminal mastermind in a corrupt city. Batman must choose between his moral code and the greater good."),
		new("The Grand Budapest Hotel", "Comedy", "A legendary concierge and his lobby boy get entangled in a murder mystery across a fictional European republic. Whimsical and fast-paced with a sharp wit."),
		new("Interstellar", "Sci-Fi", "A former NASA pilot travels through a wormhole to find a new home for humanity. Time bends and emotional bonds are tested across galaxies."),
		new("The Notebook", "Romance", "Two young lovers from different social worlds fall apart and reunite over decades. A story of enduring love told through memory and letters."),
		new("Get Out", "Horror / Thriller", "A Black man visits his white girlfriend's family estate and discovers a terrifying secret. Social commentary wrapped in pure psychological dread."),
		new("Spirited Away", "Animation / Fantasy", "A young girl gets trapped in a spirit world and must work to free herself and her parents. A magical journey about courage and identity."),
		new("The Shawshank Redemption", "Drama", "A man wrongly convicted of murder befriends a fellow prisoner and plans a slow escape. A deeply human story about hope and resilience behind bars."),
		new("Knives Out", "Mystery / Comedy", "A detective investigates the death of a wealthy crime novelist surrounded by scheming family members. A clever whodunit with sharp humor."),
		new("Parasite", "Thriller / Drama", "A poor family infiltrates the household of a wealthy family through deception. A dark twist-filled story about class inequality."),
		new("La La Land", "Musical / Romance", "Two dreamers fall in love in Los Angeles while chasing their artistic ambitions. A bittersweet story about sacrifice and what could have been."),
		new("Mad Max: Fury Road", "Action", "A lone warrior and a rebel leader race across a post-apocalyptic wasteland against a tyrant. Non-stop adrenaline with stunning practical effects."),
		new("Her", "Sci-Fi / Romance", "A lonely writer falls in love with an AI operating system in near-future Los Angeles. A quiet meditation on connection, loneliness, and what makes us human."),
		new("Coco", "Animation / Family", "A young boy accidentally enters the Land of the Dead and must find his great-great-grandfather before dawn. A vibrant celebration of family and memory."),
		new("Hereditary", "Horror", "A family unravels after the death of their secretive grandmother, uncovering a dark supernatural legacy. Slow dread builds into pure terror."),
		new("The Social Network", "Drama / Biopic", "The founding of Facebook told through betrayal, lawsuits, and obsession. A razor-sharp portrait of ambition and friendship destroyed by success."),
		new("Whiplash", "Drama", "An ambitious young drummer pushes himself to the edge under a brutal music teacher. Tension mounts with every rehearsal until the explosive finale."),
		new("Jurassic Park", "Adventure / Sci-Fi", "A theme park built around cloned dinosaurs collapses into chaos when the animals escape. Wonder and terror in equal measure."),
		new("Eternal Sunshine of the Spotless Mind", "Romance / Sci-Fi", "A couple undergoes a procedure to erase each other from memory after a painful breakup. Beautifully strange and emotionally devastating."),
		new("The Princess Bride", "Fantasy / Comedy", "A farmhand embarks on a swashbuckling adventure to rescue the woman he loves from a corrupt prince. Funny, romantic, and endlessly quotable."),
		new("Arrival", "Sci-Fi", "A linguist is recruited to communicate with alien spacecraft that have landed worldwide. A cerebral story where language and time intersect in unexpected ways."),
		new("Titanic", "Romance / Drama", "Two strangers from different classes fall in love aboard the ill-fated ocean liner. Epic romance against a backdrop of historical tragedy."),
		new("A Quiet Place", "Horror / Thriller", "A family survives in a post-apocalyptic world inhabited by creatures that hunt by sound. Tension built entirely through silence."),
		new("The Lion King", "Animation / Drama", "A young lion prince flees his kingdom after his father is murdered by his uncle. A story of guilt, identity, and reclaiming your destiny."),
		new("Pulp Fiction", "Crime / Drama", "Interconnected stories of hitmen, a boxer, and a gangster's wife unfold in nonlinear fashion. Darkly funny and relentlessly cool."),
		new("Good Will Hunting", "Drama", "A janitor with a genius-level intellect is discovered and must choose between opportunity and the comfort of his neighborhood. A quiet story about potential and healing."),
		new("The Truman Show", "Drama / Sci-Fi", "A man slowly discovers his entire life has been a television show watched by millions. A sharp satire on surveillance, reality, and free will."),
		new("Inside Out", "Animation", "The emotions inside a young girl's head struggle to help her cope after her family moves cities. A surprisingly deep exploration of sadness and growing up."),
		new("No Country for Old Men", "Thriller", "A hunter finds drug money in the desert and is relentlessly pursued by a remorseless killer. Bleak, tense, and philosophically haunting."),
		new("The Martian", "Sci-Fi / Comedy", "An astronaut is stranded alone on Mars and must science his way to survival. Optimistic, funny, and genuinely thrilling problem-solving."),
		new("Marriage Story", "Drama / Romance", "A couple navigates a painful divorce while trying to remain good parents and people. Intimate and devastating in equal parts.")
	];

	private readonly string _connectionString;
	private readonly EmbeddingClient _embeddingClient;

	public MoviesController(IConfiguration configuration, OpenAIClient openAIClient)
	{
		_connectionString = configuration.GetConnectionString("Default")
			?? throw new InvalidOperationException("Missing ConnectionStrings:Default configuration.");
		_embeddingClient = openAIClient.GetEmbeddingClient(EmbeddingModel);
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

	public sealed class SeedResponse
	{
		public int SeededCount { get; set; }
	}

	[HttpPost("seed")]
	public async Task<ActionResult<SeedResponse>> Seed(CancellationToken cancellationToken)
	{
		await using var dataSource = CreateDataSource();
		await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
		await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

		await using (var createExtension = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", connection, transaction))
		{
			await createExtension.ExecuteNonQueryAsync(cancellationToken);
		}

		await connection.ReloadTypesAsync(cancellationToken);

		await using (var deleteExisting = new NpgsqlCommand("DELETE FROM movies", connection, transaction))
		{
			await deleteExisting.ExecuteNonQueryAsync(cancellationToken);
		}

		foreach (var movie in SeedMovies)
		{
			var embeddingVector = await GenerateVectorAsync(movie.Description, cancellationToken);

			await using var insertCommand = new NpgsqlCommand(
				@"INSERT INTO movies (title, genre, description, embedding)
				  VALUES (@title, @genre, @description, @embedding)",
				connection,
				transaction);

			insertCommand.Parameters.AddWithValue("title", movie.Title);
			insertCommand.Parameters.AddWithValue("genre", movie.Genre.ToUpperInvariant());
			insertCommand.Parameters.AddWithValue("description", movie.Description);
			insertCommand.Parameters.AddWithValue("embedding", embeddingVector);

			await insertCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);

		return Ok(new SeedResponse { SeededCount = SeedMovies.Count });
	}

	[HttpPost("recommend")]
	public async Task<ActionResult<List<RecommendResponse>>> Recommend([FromBody] RecommendRequest request, CancellationToken cancellationToken)
	{
		if (request is null || string.IsNullOrWhiteSpace(request.Mood))
		{
			return BadRequest("Mood is required.");
		}

		var moodVector = await GenerateVectorAsync(request.Mood.Trim(), cancellationToken);
		var results = new List<RecommendResponse>();

		await using var dataSource = CreateDataSource();
		await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
		await using var command = new NpgsqlCommand(
			@"SELECT title, genre, description,
			         1 - (embedding <=> @queryVec::vector) AS score
			  FROM movies
			  ORDER BY embedding <=> @queryVec::vector
			  LIMIT 3;",
			connection);

		command.Parameters.Add(new NpgsqlParameter("queryVec", moodVector));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			results.Add(new RecommendResponse
			{
				Title = reader.GetString(0),
				Genre = reader.GetString(1),
				Description = reader.GetString(2),
				Score = reader.GetDouble(3)
			});
		}

		return Ok(results);
	}

	private NpgsqlDataSource CreateDataSource()
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
		dataSourceBuilder.UseVector();
		return dataSourceBuilder.Build();
	}

	private async Task<Vector> GenerateVectorAsync(string input, CancellationToken cancellationToken)
	{
		var embedding = await _embeddingClient.GenerateEmbeddingAsync(input, cancellationToken: cancellationToken);
		return new Vector(embedding.Value.ToFloats().ToArray());
	}

	private sealed record SeedMovie(string Title, string Genre, string Description);
}

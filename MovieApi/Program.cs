using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<MovieContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        o => o.UseVector()
    )
);

builder.Services.AddSingleton(new OpenAIClient(
    builder.Configuration["OpenAI:ApiKey"]
));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        })
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

var app = builder.Build();

app.UseCors("AllowReact");
app.MapControllers();
app.Run();

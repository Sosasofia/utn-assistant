using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using UtnAssistant.API.Data;
using UtnAssistant.API.Data.Seed;
using UtnAssistant.API.Endpoints;
using UtnAssistant.API.Enums;
using UtnAssistant.API.Models;
using UtnAssistant.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
        npgsqlOptions.MapEnum<CorrelativeType>("CorrelativeType");
        npgsqlOptions.MapEnum<ProgressStatus>("ProgressStatus");
    })
);


builder.Services.AddScoped<AcademicChatService>();
builder.Services.AddScoped<SyllabusIngestor>();
builder.Services.AddScoped<AcademicHistoryService>();
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<SubjectsService>();

var app = builder.Build();

// CLI Command: dotnet run -- --seed
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Running database seed...");
    await DbInitializer.SeedAsync(db, logger);
    logger.LogInformation("Seeding completed successfully.");
    return;
}

if (args.Contains("--ingest"))
{
    using var scope = app.Services.CreateScope();
    var ingestor = scope.ServiceProvider.GetRequiredService<SyllabusIngestor>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await ingestor.IngestAsync(logger);
    return;
}

if (args.Contains("--check"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await DatabaseChecker.CheckAsync(db);
    return;
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}


app.MapGet("/api/hello", () => "Hello World")
    .WithName("Hello");

app.MapChatEndpoints();
app.MapAcademicHistoryEndpoints();
app.MapUserEndpoints();
app.MapSubjectEndpoints();

app.Run();

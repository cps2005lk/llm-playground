using LlmPlayground.API.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .WriteTo.Console());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // LLM provider — Ollama via its OpenAI-compatible endpoint
    var ollamaBaseUrl = builder.Configuration["LlmProvider:BaseUrl"] ?? "http://localhost:11434";
    builder.Services.AddHttpClient<ILlmProvider, OllamaProvider>(client =>
    {
        client.BaseAddress = new Uri(ollamaBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(120);
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

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

    // Provider settings — mutable singleton so they can be changed at runtime via the API
    var providerSettings = new LlmProviderSettings
    {
        BaseUrl = (builder.Configuration["LlmProvider:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/'),
        ApiKey = builder.Configuration["LlmProvider:ApiKey"] ?? string.Empty
    };
    builder.Services.AddSingleton(providerSettings);

    // Named HttpClient with no pre-set base address; OllamaProvider builds absolute URLs at call time
    builder.Services.AddHttpClient("ollama", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(120);
    });

    builder.Services.AddScoped<ILlmProvider, OllamaProvider>();

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

using Babbelbos.Api.Development;
using Babbelbos.Api.Endpoints;
using Babbelbos.ServiceDefaults;

using BuildingBlocks.Application;

using Conversations.Application.Ports;
using Conversations.Infrastructure;

using GnomeCatalog.Infrastructure;

using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseWolverine(options =>
{
    // Each module's Application assembly holds its command and query handlers.
    options.Discovery.IncludeAssembly(typeof(Conversations.Application.AssemblyMarker).Assembly);
});

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IClock, SystemClock>();

// TEMPORARY until sign-in exists (iteration 4): tenant and participant come from configuration.
builder.Services.AddScoped<ITenantContext, ConfigurationTenantContext>();
builder.Services.AddScoped<IParticipantContext, ConfigurationParticipantContext>();

builder.Services.AddGnomeCatalogModule(builder.Configuration);
builder.Services.AddConversationsModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateConversationsDatabaseAsync();
}

app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Ok(new { name = "Babbelbos API" }));
app.MapConversationEndpoints();

await app.RunAsync();

/// <summary>Entry point, public so integration tests can host the API.</summary>
public partial class Program;

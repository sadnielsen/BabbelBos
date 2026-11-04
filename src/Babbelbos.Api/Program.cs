using Babbelbos.ServiceDefaults;

using Conversation.Infrastructure;

using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseWolverine(options =>
{
    // Each module's Application assembly holds its command and query handlers.
    options.Discovery.IncludeAssembly(typeof(Conversation.Application.AssemblyMarker).Assembly);
});

builder.Services.AddConversationModule();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Ok(new { name = "Babbelbos API" }));

await app.RunAsync();

/// <summary>Entry point, public so integration tests can host the API.</summary>
public partial class Program;

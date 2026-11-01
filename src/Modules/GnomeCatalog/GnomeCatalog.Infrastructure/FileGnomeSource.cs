using System.Text.Json;

using GnomeCatalog.Application;
using GnomeCatalog.Domain;

namespace GnomeCatalog.Infrastructure;

/// <summary>
/// Reads gnomes from <c>prompts/gnomes/&lt;id&gt;/</c> and the shared prompts from <c>prompts/shared/</c>.
/// Invalid definitions fail at startup, never during a conversation.
/// </summary>
public sealed class FileGnomeSource(string promptsDirectory) : IGnomeSource
{
    public const string SharedVersion = "v1";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public IReadOnlyList<Gnome> LoadGnomes()
    {
        var gnomesDirectory = Path.Combine(promptsDirectory, "gnomes");
        if (!Directory.Exists(gnomesDirectory))
        {
            throw new DirectoryNotFoundException($"Gnome directory not found: {gnomesDirectory}");
        }

        return Directory.GetDirectories(gnomesDirectory)
            .Order(StringComparer.Ordinal)
            .Select(LoadGnome)
            .ToList();
    }

    public SharedPrompts LoadSharedPrompts()
    {
        var shared = Path.Combine(promptsDirectory, "shared");
        return new SharedPrompts(
            File.ReadAllText(Path.Combine(shared, $"safety-rules.{SharedVersion}.md")),
            File.ReadAllText(Path.Combine(shared, $"context.{SharedVersion}.md")),
            File.ReadAllText(Path.Combine(shared, $"output-format.{SharedVersion}.md")),
            SharedVersion);
    }

    private static Gnome LoadGnome(string directory)
    {
        var definition = ReadJson<GnomeDefinition>(Path.Combine(directory, "gnome.json"));
        var persona = File.ReadAllText(Path.Combine(directory, $"persona.{definition.PersonaVersion}.md"));
        var scripts = ReadJson<Dictionary<string, string>>(
            Path.Combine(directory, $"safety-scripts.{definition.SafetyScriptsVersion}.json"));

        var result = Gnome.Create(
            definition.Id,
            definition.Name,
            definition.Greeting,
            definition.IsDefault,
            persona,
            definition.PersonaVersion,
            scripts,
            definition.SafetyScriptsVersion);

        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"Invalid gnome in {directory}: {result.Error.Message}");
    }

    private static T ReadJson<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidOperationException($"Empty file: {path}");

    private sealed record GnomeDefinition(
        string Id,
        string Name,
        string Greeting,
        bool IsDefault,
        string PersonaVersion,
        string SafetyScriptsVersion);
}

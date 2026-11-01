using GnomeCatalog.Contracts;
using GnomeCatalog.Domain;

namespace GnomeCatalog.Application;

/// <summary>
/// Builds the public <see cref="GnomeProfile"/>s once from the gnome source.
/// The system prompt is: persona + shared safety rules + context + output format.
/// </summary>
public sealed class GnomeCatalogService : IGnomeCatalog
{
    private readonly Dictionary<string, GnomeProfile> _profiles;
    private readonly GnomeProfile _default;

    public GnomeCatalogService(IGnomeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var shared = source.LoadSharedPrompts();
        var gnomes = source.LoadGnomes();

        if (gnomes.Count == 0)
        {
            throw new InvalidOperationException("No gnomes found. At least one gnome must be defined in prompts/gnomes.");
        }

        var defaults = gnomes.Where(g => g.IsDefault).ToList();
        if (defaults.Count != 1)
        {
            throw new InvalidOperationException($"Exactly one gnome must be the default, found {defaults.Count}.");
        }

        _profiles = gnomes.ToDictionary(g => g.Id, g => ToProfile(g, shared), StringComparer.OrdinalIgnoreCase);
        _default = _profiles[defaults[0].Id];
    }

    public GnomeProfile GetDefault() => _default;

    public GnomeProfile? Find(string gnomeId) => _profiles.GetValueOrDefault(gnomeId);

    private static GnomeProfile ToProfile(Gnome gnome, SharedPrompts shared)
    {
        var template = string.Join(
            "\n\n---\n\n",
            gnome.PersonaPrompt.Trim(),
            shared.SafetyRules.Trim(),
            shared.Context.Trim(),
            shared.OutputFormat.Trim());

        var version = $"{gnome.Id}/persona.{gnome.PersonaVersion}+scripts.{gnome.SafetyScriptsVersion}+shared.{shared.Version}";

        return new GnomeProfile(gnome.Id, gnome.Name, gnome.Greeting, template, version, gnome.SafetyScripts);
    }
}

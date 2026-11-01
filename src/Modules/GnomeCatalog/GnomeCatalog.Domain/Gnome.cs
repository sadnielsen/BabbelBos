using BuildingBlocks.Domain;

namespace GnomeCatalog.Domain;

/// <summary>
/// A gnome character. Gnomes are defined by the platform (as files in <c>prompts/</c>), not per tenant.
/// </summary>
public sealed class Gnome : AggregateRoot<string>
{
    public const string DefaultScriptKey = "Default";
    public const string FallbackScriptKey = "Fallback";

    private Gnome(
        string id,
        string name,
        string greeting,
        bool isDefault,
        string personaPrompt,
        string personaVersion,
        IReadOnlyDictionary<string, string> safetyScripts,
        string safetyScriptsVersion)
        : base(id)
    {
        Name = name;
        Greeting = greeting;
        IsDefault = isDefault;
        PersonaPrompt = personaPrompt;
        PersonaVersion = personaVersion;
        SafetyScripts = safetyScripts;
        SafetyScriptsVersion = safetyScriptsVersion;
    }

    public string Name { get; }

    public string Greeting { get; }

    public bool IsDefault { get; }

    public string PersonaPrompt { get; }

    public string PersonaVersion { get; }

    public IReadOnlyDictionary<string, string> SafetyScripts { get; }

    public string SafetyScriptsVersion { get; }

    /// <summary>
    /// Creates a gnome. A gnome without a default and a fallback safety script is invalid,
    /// because then the system could not give a safe answer.
    /// </summary>
    public static Result<Gnome> Create(
        string id,
        string name,
        string greeting,
        bool isDefault,
        string personaPrompt,
        string personaVersion,
        IReadOnlyDictionary<string, string> safetyScripts,
        string safetyScriptsVersion)
    {
        ArgumentNullException.ThrowIfNull(safetyScripts);

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Gnome>(GnomeErrors.MissingIdentity);
        }

        if (string.IsNullOrWhiteSpace(greeting))
        {
            return Result.Failure<Gnome>(GnomeErrors.MissingGreeting);
        }

        if (string.IsNullOrWhiteSpace(personaPrompt))
        {
            return Result.Failure<Gnome>(GnomeErrors.MissingPersona);
        }

        foreach (var key in new[] { DefaultScriptKey, FallbackScriptKey })
        {
            if (!safetyScripts.TryGetValue(key, out var script) || string.IsNullOrWhiteSpace(script))
            {
                return Result.Failure<Gnome>(GnomeErrors.MissingSafetyScript(key));
            }
        }

        return Result.Success(new Gnome(id, name, greeting, isDefault, personaPrompt, personaVersion, safetyScripts, safetyScriptsVersion));
    }
}

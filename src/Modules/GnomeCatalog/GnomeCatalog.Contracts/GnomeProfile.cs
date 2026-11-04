namespace GnomeCatalog.Contracts;

/// <summary>
/// Everything another module needs to let a gnome speak: its name, the system prompt
/// template and the fixed safety scripts, with the versions that were used.
/// </summary>
/// <param name="Id">Stable id of the gnome, for example "pim".</param>
/// <param name="Name">Display name, for example "Pim Fluister".</param>
/// <param name="Greeting">What the gnome says when a conversation starts, with <c>{{placeholders}}</c>.</param>
/// <param name="SystemPromptTemplate">Persona, shared safety rules, context and output format, with <c>{{placeholders}}</c>.</param>
/// <param name="PromptVersion">Version label stored with every gnome message, for example "pim/persona.v1+shared.v1".</param>
/// <param name="SafetyScripts">Fixed replies by key: a concern category, "Default" or "Fallback".</param>
public sealed record GnomeProfile(
    string Id,
    string Name,
    string Greeting,
    string SystemPromptTemplate,
    string PromptVersion,
    IReadOnlyDictionary<string, string> SafetyScripts)
{
    public const string DefaultScriptKey = "Default";
    public const string FallbackScriptKey = "Fallback";

    /// <summary>The script for a category, or the default script when the gnome has none for it.</summary>
    public string SafetyScriptFor(string category) =>
        SafetyScripts.TryGetValue(category, out var script) ? script : SafetyScripts[DefaultScriptKey];

    /// <summary>The reply used when a technical failure makes a normal answer impossible.</summary>
    public string FallbackScript => SafetyScripts[FallbackScriptKey];
}

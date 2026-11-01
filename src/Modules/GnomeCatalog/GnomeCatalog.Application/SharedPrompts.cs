namespace GnomeCatalog.Application;

/// <summary>The prompt parts that every gnome shares, with their version.</summary>
public sealed record SharedPrompts(string SafetyRules, string Context, string OutputFormat, string Version);

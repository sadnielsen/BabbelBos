using BuildingBlocks.Domain;

namespace GnomeCatalog.Domain;

public static class GnomeErrors
{
    public static readonly DomainError MissingIdentity = new("Gnome.MissingIdentity", "A gnome needs an id and a name.");

    public static readonly DomainError MissingGreeting = new("Gnome.MissingGreeting", "A gnome needs a greeting.");

    public static readonly DomainError MissingPersona = new("Gnome.MissingPersona", "A gnome needs a persona prompt.");

    public static DomainError MissingSafetyScript(string key) =>
        new("Gnome.MissingSafetyScript", $"A gnome needs a '{key}' safety script.");
}

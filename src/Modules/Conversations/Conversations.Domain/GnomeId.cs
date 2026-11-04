namespace Conversations.Domain;

/// <summary>Reference to a gnome from the GnomeCatalog module, for example "pim".</summary>
public readonly record struct GnomeId(string Value)
{
    public override string ToString() => Value;
}

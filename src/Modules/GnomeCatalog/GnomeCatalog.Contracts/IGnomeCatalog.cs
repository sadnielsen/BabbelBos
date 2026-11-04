namespace GnomeCatalog.Contracts;

/// <summary>Public query of the GnomeCatalog module.</summary>
public interface IGnomeCatalog
{
    /// <summary>The gnome the system suggests for a new participant.</summary>
    GnomeProfile GetDefault();

    GnomeProfile? Find(string gnomeId);
}

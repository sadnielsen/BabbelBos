using GnomeCatalog.Domain;

namespace GnomeCatalog.Application;

/// <summary>Port: where gnome definitions and shared prompts come from (files in <c>prompts/</c>).</summary>
public interface IGnomeSource
{
    IReadOnlyList<Gnome> LoadGnomes();

    SharedPrompts LoadSharedPrompts();
}

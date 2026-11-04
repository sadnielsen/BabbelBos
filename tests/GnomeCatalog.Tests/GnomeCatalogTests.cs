using GnomeCatalog.Application;
using GnomeCatalog.Contracts;
using GnomeCatalog.Domain;
using GnomeCatalog.Infrastructure;

namespace GnomeCatalog.Tests;

/// <summary>Loads the real files in prompts/, so a broken prompt fails the build, not a conversation.</summary>
public class GnomeCatalogTests
{
    private static readonly string PromptsDirectory = Path.Combine(AppContext.BaseDirectory, "prompts");

    private static GnomeCatalogService Catalog() => new(new FileGnomeSource(PromptsDirectory));

    [Fact]
    public void Pim_is_the_default_gnome()
    {
        var pim = Catalog().GetDefault();

        pim.Id.ShouldBe("pim");
        pim.Name.ShouldBe("Pim Fluister");
        pim.Greeting.ShouldContain("{{participant_name}}");
    }

    [Fact]
    public void System_prompt_contains_persona_safety_rules_context_and_output_format()
    {
        var template = Catalog().GetDefault().SystemPromptTemplate;

        template.ShouldContain("Je bent **Pim Fluister**");
        template.ShouldContain("Veiligheidsregels");
        template.ShouldContain("{{trusted_adults}}");
        template.ShouldContain("\"reply\"");
    }

    [Theory]
    [InlineData("SuicidalIdeation")]
    [InlineData("SelfHarm")]
    [InlineData("Abuse")]
    [InlineData("Neglect")]
    [InlineData("MedicalEmergency")]
    public void Every_high_risk_category_has_its_own_script(string category)
    {
        var pim = Catalog().GetDefault();

        pim.SafetyScripts.ShouldContainKey(category);
        pim.SafetyScriptFor(category).ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("Abuse")]
    [InlineData("Neglect")]
    public void Abuse_and_neglect_scripts_do_not_name_a_specific_adult(string category)
    {
        // The named adult could be the source of the concern (design §3.2, AlertRoutingPolicy).
        Catalog().GetDefault().SafetyScriptFor(category).ShouldNotContain("{{primary_trusted_adult}}");
    }

    [Fact]
    public void Prompt_version_names_every_part()
    {
        Catalog().GetDefault().PromptVersion.ShouldBe("pim/persona.v1+scripts.v1+shared.v1");
    }

    [Fact]
    public void Unknown_category_falls_back_to_the_default_script()
    {
        var pim = Catalog().GetDefault();

        pim.SafetyScriptFor("Unknown").ShouldBe(pim.SafetyScripts[GnomeProfile.DefaultScriptKey]);
    }

    [Fact]
    public void A_gnome_without_a_fallback_script_is_invalid()
    {
        var scripts = new Dictionary<string, string> { ["Default"] = "Ga naar een grote mens." };

        var result = Gnome.Create("test", "Test", "Hallo", isDefault: true, "Persona", "v1", scripts, "v1");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Gnome.MissingSafetyScript");
    }
}

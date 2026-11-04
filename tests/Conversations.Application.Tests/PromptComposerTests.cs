using Conversations.Application.Replies;

namespace Conversations.Application.Tests;

public class PromptComposerTests
{
    [Theory]
    [InlineData("2026-11-07T08:30:00Z", "zaterdagochtend")]
    [InlineData("2026-11-02T14:00:00Z", "maandagmiddag")]
    [InlineData("2026-11-04T19:00:00Z", "woensdagavond")]
    public void Describes_local_time_in_dutch(string utc, string expected)
    {
        PromptComposer.DescribeTime(DateTimeOffset.Parse(utc, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(expected);
    }

    [Fact]
    public void Unknown_placeholders_become_nothing_known()
    {
        TemplateRenderer.Render("Weet je: {{memory_facts}}", new Dictionary<string, string>())
            .ShouldBe("Weet je: (nog niets bekend)");
    }

    [Fact]
    public void Without_trusted_adults_the_gnome_refers_to_any_trusted_adult()
    {
        var values = TemplateRenderer.ParticipantValues(
            new Ports.ParticipantProfile(new Domain.ParticipantId(Guid.NewGuid()), "Sam", []),
            "Pim");

        values["primary_trusted_adult"].ShouldBe(TemplateRenderer.AnyTrustedAdult);
    }
}

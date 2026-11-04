using System.Net;

namespace Babbelbos.Api.Tests;

public class OpenApiTests(PostgresFixture postgres)
{
    [Theory]
    [InlineData("/openapi/v1.json", "/api/conversations/{conversationId}/messages")]
    [InlineData("/scalar", "<html")]
    public async Task Development_has_an_api_description_and_a_test_page(string path, string expected)
    {
        await using var factory = await BabbelbosApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain(expected);
    }
}

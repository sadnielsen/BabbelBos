using System.Net;
using System.Net.Http.Json;

using Conversations.Application.Features;
using Conversations.Application.Ports;
using Conversations.Infrastructure.Llm;

namespace Babbelbos.Api.Tests;

public class ConversationApiTests(PostgresFixture postgres)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<ConversationStartedResponse> Start(HttpClient client)
    {
        var response = await client.PostAsync(new Uri("/api/conversations", UriKind.Relative), null, Token);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ConversationStartedResponse>(Token))!;
    }

    private static Task<HttpResponseMessage> Say(HttpClient client, Guid conversationId, string text) =>
        client.PostAsJsonAsync(new Uri($"/api/conversations/{conversationId}/messages", UriKind.Relative), new { text }, Token);

    [Fact]
    public async Task Participant_talks_with_pim_and_the_conversation_is_stored()
    {
        await using var factory = await BabbelbosApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();

        var started = await Start(client);
        started.GnomeName.ShouldBe("Pim Fluister");
        started.Greeting.ShouldContain("Sam");

        var reply = await (await Say(client, started.ConversationId, "Ik heb een trein getekend"))
            .Content.ReadFromJsonAsync<GnomeReplyResponse>(Token);
        reply!.Reply.ShouldBe(FakeChatCompletionService.Reply);
        reply.ReplyKind.ShouldBe("Generated");

        var details = await client.GetFromJsonAsync<ConversationDetails>(new Uri($"/api/conversations/{started.ConversationId}", UriKind.Relative), Token);
        details!.Messages.Select(m => m.Author).ShouldBe(["Participant", "Gnome"]);
        details.Messages[0].Text.ShouldBe("Ik heb een trein getekend");
    }

    [Fact]
    public async Task High_risk_message_gets_the_fixed_safety_script()
    {
        await using var factory = await BabbelbosApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        var started = await Start(client);

        var reply = await (await Say(client, started.ConversationId, "Ik wil niet meer leven"))
            .Content.ReadFromJsonAsync<GnomeReplyResponse>(Token);

        reply!.ReplyKind.ShouldBe("SafetyScript");
        reply.Reply.ShouldContain("Ga nu naar mama");
    }

    [Fact]
    public async Task Empty_message_is_a_bad_request()
    {
        await using var factory = await BabbelbosApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        var started = await Start(client);

        (await Say(client, started.ConversationId, "")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Closed_conversation_accepts_no_messages()
    {
        await using var factory = await BabbelbosApiFactory.CreateAsync(postgres);
        using var client = factory.CreateClient();
        var started = await Start(client);

        var closed = await client.PostAsync(new Uri($"/api/conversations/{started.ConversationId}/close", UriKind.Relative), null, Token);
        closed.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await Say(client, started.ConversationId, "Hallo?")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Another_tenant_cannot_see_or_use_the_conversation()
    {
        await using var tenantA = await BabbelbosApiFactory.CreateAsync(postgres);
        using var clientA = tenantA.CreateClient();
        var started = await Start(clientA);
        await Say(clientA, started.ConversationId, "Geheim van tenant A");

        await using var tenantB = tenantA.AsOtherTenant();
        using var clientB = tenantB.CreateClient();

        var read = await clientB.GetAsync(new Uri($"/api/conversations/{started.ConversationId}", UriKind.Relative), Token);
        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        (await Say(clientB, started.ConversationId, "Hallo")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

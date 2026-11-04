using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Conversations.Application.Ports;
using Conversations.Application.Replies;
using Conversations.Domain;

using GnomeCatalog.Contracts;

namespace Conversations.Application.Features;

/// <summary>Starts a conversation between the current participant and their gnome.</summary>
public sealed record StartConversation;

public sealed record ConversationStartedResponse(Guid ConversationId, string GnomeId, string GnomeName, string Greeting);

public sealed class StartConversationHandler(
    IConversationRepository conversations,
    ITenantContext tenant,
    IParticipantContext participantContext,
    IGnomeCatalog gnomes,
    IClock clock)
{
    public async Task<Result<ConversationStartedResponse>> Handle(StartConversation command, CancellationToken cancellationToken)
    {
        var participant = participantContext.Current;

        // Until participants have an assigned gnome (Household module), everyone talks to the default gnome.
        var gnome = gnomes.GetDefault();

        var conversation = Conversation.Start(tenant.TenantId, participant.Id, new GnomeId(gnome.Id), clock.UtcNow);
        conversations.Add(conversation);
        await conversations.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var greeting = TemplateRenderer.Render(gnome.Greeting, TemplateRenderer.ParticipantValues(participant, gnome.Name));
        return Result.Success(new ConversationStartedResponse(conversation.Id.Value, gnome.Id, gnome.Name, greeting));
    }
}

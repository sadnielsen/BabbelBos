using BuildingBlocks.Domain;

namespace Conversations.Domain;

// Domain events carry ids and safety outcomes only, never message text (see CLAUDE.md, Privacy).

public sealed record ConversationStarted(
    ConversationId ConversationId,
    TenantId TenantId,
    ParticipantId ParticipantId,
    GnomeId GnomeId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ParticipantMessageReceived(
    ConversationId ConversationId,
    TenantId TenantId,
    ParticipantId ParticipantId,
    MessageId MessageId,
    SafetyAssessment Safety,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record GnomeReplied(
    ConversationId ConversationId,
    MessageId MessageId,
    ReplyKind Kind,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ConversationClosed(
    ConversationId ConversationId,
    DateTimeOffset OccurredAt) : IDomainEvent;

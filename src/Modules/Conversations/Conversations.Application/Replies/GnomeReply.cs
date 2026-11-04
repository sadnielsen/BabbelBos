using Conversations.Domain;

namespace Conversations.Application.Replies;

/// <summary>The reply the gnome will give, with how it was made and its safety outcome.</summary>
public sealed record GnomeReply(string Text, ReplyKind Kind, SafetyAssessment Safety);

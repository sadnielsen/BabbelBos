using BuildingBlocks.Domain;

using Conversations.Domain;

namespace Babbelbos.Api.Endpoints;

/// <summary>Maps domain results to HTTP responses. No business logic here.</summary>
internal static class ResultHttpExtensions
{
    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : Problem(result.Error);

    public static IResult ToHttp(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : Problem(result.Error);

    private static IResult Problem(DomainError error)
    {
        var status = error.Code switch
        {
            _ when error == ConversationErrors.NotFound => StatusCodes.Status404NotFound,
            _ when error == ConversationErrors.EmptyMessage || error == ConversationErrors.MessageTooLong => StatusCodes.Status400BadRequest,
            _ when error == ConversationErrors.Closed || error == ConversationErrors.Full || error == ConversationErrors.AwaitingGnomeReply => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(title: error.Message, statusCode: status, extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}

using Conversations.Application.Ports;
using Conversations.Domain;

namespace Babbelbos.Api.Development;

/// <summary>
/// TEMPORARY until the Household and Identity modules (iteration 4): one fictional participant
/// from configuration (<c>Development:Participant</c>). Never put real participant data here.
/// </summary>
public sealed class ConfigurationParticipantContext(IConfiguration configuration) : IParticipantContext
{
    public ParticipantProfile Current
    {
        get
        {
            var section = configuration.GetSection("Development:Participant");
            if (!Guid.TryParse(section["Id"], out var id) || string.IsNullOrWhiteSpace(section["DisplayName"]))
            {
                throw new InvalidOperationException("No participant: Development:Participant is not configured and there is no sign-in yet.");
            }

            return new ParticipantProfile(
                new ParticipantId(id),
                section["DisplayName"]!,
                section.GetSection("TrustedAdults").Get<string[]>() ?? [],
                section["AgeGroup"],
                section["DevelopmentNotes"]);
        }
    }
}

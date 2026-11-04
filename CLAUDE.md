# CLAUDE.md — Babbelbos

Instructies voor AI-codeerassistenten (Claude Code, Copilot, Cursor e.d.) die in deze repository werken. Lees dit eerst. Het volledige ontwerp staat in `docs/01-application-design.md`; de kabouter-prompt in `docs/02-kabouter-system-prompt.md` (wordt in iteratie 2 opgesplitst naar `prompts/`).

## Wat is dit

Een multi-tenant SaaS waarin jonge kinderen (kleuters) en mensen met een beperking met een vergelijkbare ontwikkelingsleeftijd praten met een kabouter (LLM). Ouders, voogden en begeleiders (ondersteuners) beheren deelnemers en krijgen meldingen bij zorgsignalen; bij een vermoeden van misbruik gaat de melding naar een vertrouwenspersoon. De deelnemers kunnen niet lezen of schrijven: de core werkt met tekst, de deelnemer-client met spraak en beeld.

Het is óók een leerproject: kies de "nette" oplossing (DDD, CQRS, Clean Architecture, SOLID, CI/CD) boven de snelste, maar houd het simpel waar het kan (YAGNI).

## Taal

- **Code, identifiers, commits, PR's**: Engels.
- **Domeintermen**: gebruik de Engelse namen uit de woordenlijst hieronder, consequent.
- **Documentatie in `docs/` en prompts in `prompts/`**: Nederlands.
- **Teksten die de kabouter zegt**: Nederlands, kleutertaal.

| NL | Code |
|---|---|
| Kabouter | `Gnome` |
| Deelnemer (kind of persoon met beperking) | `Participant` |
| Ondersteuner (persoon met account) | `Caregiver` |
| Steunrelatie | `SupportRelation` (type `Staff` = begeleider, `Guardian` = ouder/voogd/curator/mentor) |
| Afdeling / woning | `CareUnit` |
| Vertrouwenspersoon | `ConfidentialAdviser` (scope CareUnit / Tenant / Platform) |
| Gesprek | `Conversation` |
| Bericht | `Message` |
| Kaboutergeheugen | `GnomeMemory`, `RememberedFact` |
| Zorgsignaal | `Concern` |
| Melding | `Alert` |
| Instelling/gezin | `Tenant` |

Gebruik nooit `Child`, `Companion`, `User` of `ConversationMemory` voor deze begrippen.

## Stack

- .NET 10, C# (nullable enabled, warnings as errors), ASP.NET Core Minimal APIs
- Wolverine (mediator, messaging, outbox, achtergrondtaken)
- PostgreSQL + EF Core (write) / Dapper of projecties (read)
- Microsoft.Extensions.AI voor LLM's (lokaal Ollama)
- Keycloak (OIDC) lokaal
- .NET Aspire voor lokaal draaien; Docker + Helm/k3d voor k8s
- Tests: xUnit v3 (Microsoft.Testing.Platform), Shouldly (of FluentAssertions-alternatief), NSubstitute, Testcontainers, NetArchTest
- Frontend: PWA in `clients/` (web-participant, web-caregiver)

## Commando's

```bash
dotnet build Babbelbos.slnx                                   # warnings zijn errors (Directory.Build.props)
dotnet test --solution Babbelbos.slnx                         # alle tests (xUnit v3 op Microsoft.Testing.Platform)
dotnet format Babbelbos.slnx --verify-no-changes              # formatting check, zoals in CI
dotnet run --project src/Babbelbos.AppHost                    # alles lokaal via Aspire (Docker nodig voor PostgreSQL)
docker build -f src/Babbelbos.Api/Dockerfile -t babbelbos-api .   # container image van de API
```

Package-versies staan centraal in `Directory.Packages.props`; in een `.csproj` staat alleen `<PackageReference Include="..." />` zonder versie.

> Werk deze sectie bij zodra de commando's bestaan of veranderen.

## Architectuur — regels die je niet breekt

1. **Modulaire monoliet.** Modules staan in `src/Modules/<Module>/` met lagen `Domain`, `Application`, `Infrastructure`, `Contracts`.
2. **Afhankelijkheden wijzen naar binnen.** `Domain` heeft geen referenties (ook niet naar EF, Wolverine of ASP.NET). `Application` kent alleen `Domain` en `BuildingBlocks`. `Infrastructure` implementeert interfaces uit `Application`.
3. **Modules praten via `Contracts`** (integration events, publieke queries). Nooit een andere module zijn `Domain`, `Infrastructure` of tabellen gebruiken.
4. **Elke module heeft een eigen DB-schema** (`conversation`, `safeguarding`, `household`, …).
5. Deze regels zijn afgedwongen in `tests/Architecture.Tests`. Pas die tests niet aan om een overtreding te laten slagen; pas de code aan.

## DDD

- Aggregates beschermen hun invarianten; geen publieke setters. Wijzigingen via methoden met een betekenisvolle naam (`conversation.AddParticipantMessage(...)`, niet `conversation.Messages.Add(...)`).
- Gebruik value objects voor ID's (`ParticipantId`, `TenantId` als `readonly record struct`) en voor begrippen met regels (`SafetyAssessment`, `PictureSequence`).
- Aggregates verwijzen naar elkaar via ID, niet via navigatieproperties.
- Domeinfouten via een `Result`/`Result<T>`-type; exceptions alleen voor echt onverwachte situaties.
- Aggregates verzamelen domain events; die worden na het opslaan gepubliceerd via de Wolverine-outbox.

## CQRS

- Commands: `record` met de naam in de gebiedende wijs (`SendParticipantMessage`). Eén handler per command. Handlers zijn dun: laden, aggregate-methode aanroepen, opslaan.
- Queries: `record` met `Get…`/`List…`. Queries raken geen aggregates en geen domeinlogica; ze lezen direct en geven DTO's terug.
- Validatie met FluentValidation via Wolverine-middleware.
- Eén bestand per feature is prima (command + validator + handler + endpoint bij elkaar, "vertical slice" binnen de Application-laag).

## Multi-tenancy

- **Elke** tenant-gebonden entiteit heeft `TenantId`. Uitzondering: `Caregiver` is een persoon en is niet tenant-gebonden; zijn rechten per tenant komen uit `SupportRelation`, `CareUnit`-lidmaatschap en `TenantMembership`. Nieuwe tabel? Dan ook: EF global query filter én een RLS-policy in de migratie.
- Haal de tenant alleen uit `ITenantContext`, nooit uit request-body of query string.
- Schrijf bij elke nieuwe query een integratietest die bewijst dat tenant B de data van tenant A niet ziet.

## LLM en prompts

- Roep een LLM alleen aan via de ports in `Application` (`IChatCompletionService`, `ISafetyClassifier`, `IConversationSummarizer`). Nooit een SDK van een provider direct gebruiken buiten `Infrastructure`.
- Prompts staan als bestanden in `prompts/` met een versie in de bestandsnaam (`v1.md`). **Wijzig nooit een bestaande versie**; maak `v2.md` en zet de configuratie om. Sla de gebruikte promptversie op bij elk kabouter-bericht.
- Elke wijziging in `prompts/` vereist dat de prompt-evals (`tests/Prompt.Evals`) slagen.
- Unit tests gebruiken een fake LLM (deterministisch). Geen echte LLM-aanroepen in unit- of integratietests.

## Veiligheid (dit is het belangrijkste onderdeel van de app)

- Elk bericht van een deelnemer gaat door `Safeguarding` **vóór** de LLM; elk antwoord van de kabouter **na** de LLM. Er bestaat geen codepad dat dit overslaat. Als je zo'n pad ziet of nodig denkt te hebben: stop en vraag het.
- Bij `Severity` High/Critical gebruikt de kabouter het vaste script uit `prompts/gnomes/<gnome>/safety-scripts.*.json`, nooit vrije LLM-tekst.
- Faalt de veiligheidscontrole technisch (timeout, fout)? Dan **fail closed**: generiek veilig antwoord + log + metric. Nooit "dan maar doorlaten".
- Verlaag nooit drempels, verwijder nooit trefwoorden en maak nooit veiligheidstests "skip" om een build groen te krijgen.
- Wie een melding krijgt, bepaalt alleen `AlertRoutingPolicy`. Abuse en Neglect gaan **alleen** naar de vertrouwenspersoon (CareUnit → Tenant → Platform), nooit naar ondersteuners. Elke wijziging hieraan heeft tests per categorie nodig.
- De centrale vertrouwenspersoon kijkt over tenantgrenzen heen. Dat gaat alleen via een expliciet break-glass-pad dat elke inzage logt; nooit door de tenantfilter uit te zetten.
- Een melding (`ConcernRaised`) moet betrouwbaar aankomen: via de outbox, met retries, en een alert als aflevering faalt.

## Privacy

- **Nooit** berichttekst, namen of andere persoonsgegevens loggen. Log ID's (`ConversationId`, `MessageId`, `TenantId`).
- Geen echte kindergegevens in testdata, seed-data, fixtures of voorbeelden. Gebruik verzonnen namen.
- Geen nieuwe externe dienst (LLM, spraak, analytics) toevoegen zonder dat in de PR te benoemen; data moet in de EU kunnen blijven.

## Tests

- Domein: unit tests per invariant (`Conversation_rejects_message_when_closed`).
- Application: handler-tests met in-memory fakes.
- Integratie: Testcontainers PostgreSQL; tenant-isolatie; outbox.
- Architectuur: NetArchTest.
- Prompt-evals: `tests/Prompt.Evals/cases/*.yaml` met invoer en verwachte eigenschappen (bijv. `severity >= High`, `reply.words <= 40`, `mentions_trusted_adult`).
- Schrijf eerst een falende test bij een bug.

## Werkwijze

- Kleine PR's, één onderwerp. Conventional Commits (`feat(conversation): …`, `fix(safeguarding): …`).
- Leg belangrijke keuzes vast als ADR in `docs/adr/NNNN-titel.md` (Nederlands, kort: context, besluit, gevolgen).
- Werk dit bestand bij als een regel verandert of er een nieuw commando bij komt.
- Twijfel over domein of veiligheid? Vraag het, in plaats van te gokken.
- Geen nieuwe NuGet-pakketten zonder reden in de PR-beschrijving.

## Niet doen

- Geen event sourcing in de MVP (dat komt later met Marten).
- Geen microservices opsplitsen in de MVP.
- Geen `DbContext` buiten `Infrastructure`.
- Geen logica in endpoints; die roepen alleen Wolverine aan.
- Geen `async void`, geen `.Result`/`.Wait()`.
- Geen secrets in de repo; lokaal user-secrets/`.env` (genegeerd), in k8s via secrets/External Secrets.

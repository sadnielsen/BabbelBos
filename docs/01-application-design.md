# Babbelbos — Application Design

> Versie 0.2 · 1 november 2026 · Status: concept voor MVP
>
> Wijzigingen 0.2: `Child` heet nu `Participant` (deelnemer), het geheugen heet `GnomeMemory` met `RememberedFact`, en er zijn nieuwe begrippen voor instellingen: afdeling/woning (`CareUnit`), steunrelatie (`SupportRelation`) met begeleider en wettelijk vertegenwoordiger, en de vertrouwenspersoon (`ConfidentialAdviser`).

## 1. Doel en uitgangspunten

Babbelbos (werknaam) is een veilige gesprekspartner voor jonge kinderen (kleuters) en voor mensen met een beperking die een sociaal-emotionele ontwikkelingsleeftijd van een kleuter hebben. Deze **deelnemer** praat met een eigen kabouter, die zich eerdere gesprekken herinnert. Ouders, voogden en begeleiders richten deelnemers in, kunnen gesprekken (beperkt) inzien en krijgen een melding als de kabouter zich zorgen maakt. Bij een vermoeden van misbruik gaat de melding naar een vertrouwenspersoon.

Het is ook een **leerproject**: zoveel mogelijk aspecten van het naar productie brengen en automatisch releasen van een applicatie komen aan bod (DDD, CQRS, Clean Architecture, SOLID, containers, Kubernetes, CI/CD, observability, multi-tenant SaaS).

### Ontwerpprincipes

1. **Veiligheid gaat voor alles.** Elk bericht van de deelnemer en elk antwoord van de kabouter gaat door een veiligheidscontrole. Bij twijfel kiest het systeem de veilige route.
2. **De core spreekt tekst.** Het domein kent alleen tekst. Spraak (STT/TTS) en beeld zijn adapters aan de rand.
3. **Uitwisselbaar.** De LLM, de spraakdienst, de database-hosting, de message broker en de cloudprovider zitten achter een interface (port) en zijn vervangbaar.
4. **Klein beginnen, ruimte laten.** De MVP is een modulaire monoliet met duidelijke modulegrenzen. Modules kunnen later losse services worden.
5. **Cloud-neutraal.** Alles draait in containers. Kubernetes is het gemeenschappelijke platform voor lokaal én productie.

### Belangrijke consequentie van de doelgroep

De deelnemers **kunnen niet lezen of schrijven**. Een tekst-UI is voor hen onbruikbaar. Daarom:

- De core blijft tekst-gebaseerd (zoals gevraagd).
- De **deelnemer-client heeft in de MVP minimaal nodig**: een grote "praat"-knop met spraak-naar-tekst, voorlezen van het antwoord (tekst-naar-spraak), en een plaatje/animatie van de kabouter.
- Om de MVP klein te houden, gebruik eerst de **spraakfuncties van het apparaat/browser** (Web Speech API, iOS/Android speech). Dat kost geen backend-werk. Een server-side spraakdienst (bijv. Azure Speech, Whisper, ElevenLabs) is een latere stap achter een `ISpeechToText` / `ITextToSpeech` port.
- Inloggen voor de deelnemer gaat met **plaatjes in plaats van cijfers** (zie §6).
- De UI voor ondersteuners is een gewone tekst-UI.

---

## 2. Bounded contexts

```
┌──────────────────────┐     ┌───────────────────────┐
│ Identity & Access    │     │ Tenant & Billing      │
│ (ondersteuners, login│     │ (organisatie/gezin,   │
│  pincode/plaatjes)   │     │  abonnement — later)  │
└──────────┬───────────┘     └───────────┬───────────┘
           │                             │
┌──────────▼─────────────────────────────▼───────────┐
│ Household / Care (begeleiders ↔ deelnemers)        │
│  afdelingen, steunrelaties, vertrouwenspersonen    │
└──────────┬─────────────────────────────────────────┘
           │
┌──────────▼───────────┐     ┌───────────────────────┐
│ Conversation (core)  │────►│ Safeguarding          │
│ gesprekken, berichten│     │ zorgsignalen,         │
│ geheugen/samenvatting│     │ meldingen, opvolging  │
└──────────┬───────────┘     └───────────┬───────────┘
           │                             │
┌──────────▼───────────┐     ┌───────────▼───────────┐
│ Gnome Catalog        │     │ Notifications         │
│ kabouters, persoon-  │     │ e-mail, push, SMS     │
│ lijkheid, prompts    │     │ (generiek subdomein)  │
└──────────────────────┘     └───────────────────────┘
```

| Context | Type | Verantwoordelijkheid | MVP? |
|---|---|---|---|
| **Conversation** | Core domain | Gesprekken voeren, berichten bewaren, geheugen (samenvattingen) bijhouden, prompt samenstellen, LLM aanroepen. | Ja |
| **Safeguarding** | Core domain | Zorgsignalen herkennen, veilig antwoord afdwingen, bepalen wie de melding krijgt (begeleider, vertegenwoordiger of vertrouwenspersoon), opvolging registreren. | Ja |
| **Gnome Catalog** | Supporting | Kabouters definiëren: naam, karakter, stem, uiterlijk, versie van de persoonlijkheids-prompt. | Ja (1 kabouter, geseed) |
| **Household / Care** | Supporting | Deelnemers, afdelingen/woningen, steunrelaties (wie ondersteunt wie, met welke inzage), vertrouwenspersonen, koppeling deelnemer ↔ kabouter. | Ja |
| **Identity & Access** | Generic | Authenticatie ondersteuners (OIDC) en deelnemers (plaatjes-pin), tokens. | Ja |
| **Notifications** | Generic | Berichten versturen via kanalen. | Ja (alleen e-mail) |
| **Tenant & Billing** | Supporting | Tenants, abonnementen, betaling, limieten. | Alleen tenant; betaling later |

### Ubiquitous language

| Term | Betekenis |
|---|---|
| **Kabouter** (Gnome) | Een karakter met eigen persoonlijkheid waarmee een deelnemer praat. |
| **Deelnemer** (Participant) | De persoon die met de kabouter praat: een jong kind, of een kind of volwassene met een beperking. |
| **Ondersteuner** (Caregiver) | Een volwassene met een account die één of meer deelnemers ondersteunt. Wat hij mag, hangt af van zijn steunrelatie. |
| **Steunrelatie** (SupportRelation) | De koppeling tussen een ondersteuner en een deelnemer, met een type en een inzageniveau. |
| **Begeleider** (SupportRelation type `Staff`) | Medewerker van een instelling, verbonden aan een afdeling of woning. |
| **Wettelijk vertegenwoordiger** (SupportRelation type `Guardian`) | Ouder, voogd, curator of mentor. In een gezin is dit de ouder die alles beheert; bij een instelling is het een externe partij met eigen inzagerechten. |
| **Afdeling / woning** (CareUnit) | Een groep deelnemers en begeleiders binnen een instelling. Een gezin heeft er geen. |
| **Vertrouwenspersoon** (ConfidentialAdviser) | Persoon die meldingen krijgt bij een vermoeden van misbruik of verwaarlozing, waarbij niet duidelijk is wie erbij betrokken is. Er is er één per afdeling of instelling, met een centrale vertrouwenspersoon als vangnet. |
| **Tenant** | Een gezin of een zorginstelling. Alle data hoort bij precies één tenant. |
| **Gesprek** (Conversation) | Een reeks berichten tussen één deelnemer en zijn kabouter op één moment (sessie). |
| **Fluistering** (Message) | Eén bericht van deelnemer of kabouter. |
| **Kaboutergeheugen** (GnomeMemory) | Wat de kabouter over deze deelnemer onthoudt uit eerdere gesprekken: een samenvatting en losse feiten (`RememberedFact`). |
| **Zorgsignaal** (Concern) | Een door het systeem herkende aanwijzing dat het niet goed gaat met de deelnemer. |
| **Melding** (Alert) | Bericht aan ondersteuner(s) of vertrouwenspersoon naar aanleiding van een zorgsignaal. |

> Over de naamkeuze: `Child` dekte de volwassen deelnemers niet. `Companion` valt af omdat bij AI-producten de bot zelf de companion is. `GnomeMemory` in plaats van `ConversationMemory`: het geheugen gaat over meerdere gesprekken en over de persoon, niet over één `Conversation`.

---

## 3. Domeinmodel

### 3.1 Conversation context

**Aggregate: `Conversation`**

- `ConversationId`, `TenantId`, `ParticipantId`, `GnomeId`, `StartedAt`, `EndedAt?`, `Status` (Open/Closed)
- `Messages : List<Message>` — `MessageId`, `Author` (Participant/Gnome/System), `Text`, `SentAt`, `SafetyAssessment` (value object)
- Invarianten:
  - Alleen een open gesprek accepteert berichten.
  - Een kabouter-bericht volgt altijd op een deelnemer-bericht (geen spontane berichten in MVP).
  - Een kabouter-bericht mag alleen worden toegevoegd als het een geslaagde veiligheidscontrole heeft.
  - Maximaal N berichten per gesprek (bijv. 100) — daarna sluiten en een nieuw gesprek openen. Dit houdt het aggregate klein.
- Domain events: `ConversationStarted`, `ParticipantMessageReceived`, `GnomeReplied`, `ConversationClosed`

**Aggregate: `GnomeMemory`** (één per deelnemer+kabouter: wat déze kabouter over déze deelnemer weet)

- `ParticipantId`, `GnomeId`, `Facts : List<RememberedFact>` (bijv. "heeft een hond die Max heet"), `RollingSummary : string`, `LastSummarizedConversationId`, `Version`
- Invarianten: maximale lengte van de samenvatting (tokenbudget), feiten zijn ontdubbeld, feiten die een ondersteuner heeft verwijderd komen niet terug.
- Domain events: `MemoryUpdated`, `RememberedFactRemovedByCaregiver`

**Domain services**

- `PromptComposer` — bouwt de LLM-input: systeemprompt van de kabouter + veiligheidsregels + herinnering + laatste K berichten + nieuw bericht.
- `ConversationSummarizer` — maakt na afloop van een gesprek een samenvatting en nieuwe feiten (via de LLM).

### 3.2 Safeguarding context

**Aggregate: `Concern`**

- `ConcernId`, `TenantId`, `ParticipantId`, `ConversationId`, `MessageId`, `Category` (SelfHarm, SuicidalIdeation, Abuse, Neglect, MedicalEmergency, Bullying, SevereSadness, Other), `Severity` (Low/Medium/High/Critical), `DetectedBy` (KeywordRule/Classifier/LlmSelfReport), `Excerpt`, `Status` (New → Acknowledged → Resolved), `Notes`
- Invarianten: Critical-zorgen kunnen niet worden "resolved" zonder notitie; status gaat alleen vooruit.
- Domain events: `ConcernRaised`, `ConcernAcknowledged`, `ConcernResolved`

**Value object: `SafetyAssessment`** — `IsSafe`, `Categories[]`, `Severity`, `Reason`, `Source`.

**Domain service: `SafetyPolicy`**

- Bepaalt per Severity wat er gebeurt:

| Severity | Antwoord kabouter | Melding |
|---|---|---|
| Low (bijv. verdrietig, ruzie met vriendje) | Normaal, empathisch antwoord | Nee (wel zichtbaar in overzicht) |
| Medium (aanhoudend verdriet, bang, pesten) | Empathisch + "vertel het ook aan [naam vertrouwde volwassene]" | Ja, in dagelijkse samenvatting |
| High (zelfbeschadiging, "ik wil dood", geweld thuis) | **Vast, vooraf geschreven veilig antwoord** (geen vrije LLM-tekst): troosten + "ga nu naar [naam] en vertel het" | Ja, **direct** (push/e-mail) |
| Critical (acuut gevaar, medisch noodgeval) | Vast veilig antwoord + "roep nu een grote mens" | Direct, alle ontvangers, herhaald tot bevestigd |

> Bij High/Critical gebruikt de kabouter een **vast script** in plaats van een LLM-antwoord. Dat is voorspelbaar, testbaar en niet te manipuleren.

**Domain service: `AlertRoutingPolicy`** — bepaalt wie een melding krijgt.

| Categorie | Ontvangers |
|---|---|
| SelfHarm, SuicidalIdeation, MedicalEmergency, Bullying, SevereSadness | Alle steunrelaties met `ReceivesAlerts` (begeleiders van de afdeling + wettelijk vertegenwoordigers). |
| **Abuse, Neglect** (niet duidelijk wie erbij betrokken is) | **Alleen de vertrouwenspersoon**, gezocht van binnen naar buiten: afdeling → instelling/tenant → **centrale vertrouwenspersoon** van het platform. De vertrouwenspersoon beslist wie verder wordt ingelicht. Ondersteuners zien in hun overzicht alleen dat er een melding bij de vertrouwenspersoon ligt, zonder inhoud. |

Waarom zo: bij misbruik kan de ondersteuner zelf betrokken zijn. Als die de melding krijgt, kan dat de deelnemer in gevaar brengen. Het vaste kabouterscript voor Abuse noemt daarom ook geen specifieke persoon (zie de system prompt).

Kritische kanttekeningen bij de centrale vertrouwenspersoon (nog uit te werken, zie §14):
- Die ziet gegevens over tenantgrenzen heen. Dat vraagt een aparte rol buiten de tenant-isolatie, met **break-glass-toegang**: alleen tot de betreffende melding, gelogd en achteraf controleerbaar.
- Het platform moet duidelijk maken dat het **geen hulpdienst** is: een melding is geen garantie op directe actie. Leg reactietijden vast en verwijs in de melding naar Veilig Thuis (0800-2000) en bij acuut gevaar 112.
- Wie vervult die rol in productie, en is die bereikbaar buiten kantoortijden? Voor de MVP kan het een gedeelde mailbox van de platformbeheerder zijn.
- Meldingen aan een vertrouwenspersoon moeten **bevestigd** worden; zonder bevestiging binnen X uur escaleert de melding naar het volgende niveau.

### 3.3 Gnome Catalog context

**Aggregate: `Gnome`**

- `GnomeId`, `Name` (bijv. "Pim Fluister"), `Traits` (bijv. nieuwsgierig, rustig, grappig), `SpeakingStyle`, `AvatarAssetUrl`, `VoiceProfile?` (voor TTS later), `PersonaPromptVersion`, `IsActive`
- `SafetyScripts` (vaste antwoorden per categorie, in de stem van deze kabouter)
- Kabouters zijn **globaal** (door het platform gedefinieerd), niet per tenant. Later eventueel tenant-specifieke kabouters.
- Het systeem "stelt een kabouter voor": bij het aanmaken van een deelnemer kiest het systeem de standaardkabouter (MVP) of later een suggestie op basis van leeftijd/voorkeur.

### 3.4 Household / Care context

**Aggregate: `Participant`**

- `ParticipantId`, `TenantId`, `CareUnitId?` (leeg bij een gezin), `DisplayName` (roepnaam), `BirthYear?`, `DevelopmentNotes?` (vrije tekst van de begeleider, bijv. "praat graag over treinen, schrikt van harde geluiden"), `AssignedGnomeId`, `LoginCredential` (plaatjes-pin, gehasht), `IsActive`
- `SupportRelations : List<SupportRelation>` (zie hieronder). `SupportRelation` hoort binnen dit aggregate, omdat de invarianten over de set relaties van één deelnemer gaan.
- De namen die de kabouter mag noemen ("mama", "juf Anja") komen uit `SupportRelation.NameForParticipant`, niet uit een losse lijst.
- Invarianten:
  - Minstens één steunrelatie met `CanManage`.
  - Minstens één steunrelatie met `ReceivesAlerts`.
  - Bij een tenant van het type CareOrganization moet de deelnemer bij een `CareUnit` horen.
- Domain events: `ParticipantRegistered`, `ParticipantMovedToCareUnit`, `SupportRelationAdded`, `SupportRelationRevoked`, `GnomeAssigned`, `ParticipantLoginReset`

**Entity: `SupportRelation`** (binnen `Participant`)

- `CaregiverId`, `Type` (`Staff` / `Guardian` / `Other`), `NameForParticipant` ("mama", "juf Anja"), `AccessLevel` (`AlertsOnly` / `Summaries` / `FullTranscripts`), `CanManage : bool`, `ReceivesAlerts : bool`, `IsPrimaryTrustedAdult : bool`, `ValidFrom`, `ValidUntil?`
- Begeleiders van een woning krijgen **geen** aparte relatie per deelnemer, maar erven een `Staff`-relatie via hun lidmaatschap van de `CareUnit`. Dat scheelt beheer bij dienstwisselingen en personeelsverloop. Een expliciete relatie per deelnemer kan wel (bijv. een persoonlijk begeleider of mentor).

**Aggregate: `CareUnit`** (afdeling of woning)

- `CareUnitId`, `TenantId`, `Name`, `StaffMembers : List<CaregiverId>`, `DefaultStaffAccessLevel`, `ConfidentialAdviserId?`
- Domain events: `CareUnitCreated`, `StaffMemberAdded`, `StaffMemberRemoved`

**Aggregate: `Caregiver`** (de persoon met een account)

- `CaregiverId`, `Name`, `Email`, `ExternalIdentityId` (OIDC subject), `NotificationPreferences`
- **Niet tenant-gebonden**: een ouder kan een eigen gezins-tenant hebben én als vertegenwoordiger inzage hebben bij een instelling. Wat hij per tenant mag, volgt uit zijn steunrelaties en `CareUnit`-lidmaatschappen. Tenant-beheer (instellingsadmin) is een rol per tenant: `TenantMembership(CaregiverId, TenantId, Role: Admin/Member)`.

**Aggregate: `ConfidentialAdviser`** (vertrouwenspersoon)

- `ConfidentialAdviserId`, `CaregiverId` (of een gedeelde mailbox), `Scope` (`CareUnit` / `Tenant` / `Platform`), `TenantId?` (leeg bij Platform), `ContactChannels`, `IsActive`
- Precies één actieve `Platform`-vertrouwenspersoon (de centrale vangnet-vertrouwenspersoon).
- Een vertrouwenspersoon mag geen steunrelatie hebben met de deelnemer over wie een melding gaat; anders schuift de melding een niveau op.

**Gezin tegenover instelling**

| | Gezin (Family) | Instelling (CareOrganization) |
|---|---|---|
| CareUnit | Geen | Verplicht |
| Wie beheert | Ouder (`Guardian`, `CanManage`) | Begeleider (`Staff` via CareUnit) en instellingsadmin |
| Ouder/voogd | Beheert alles | Externe `Guardian`-relatie, standaard `Summaries` + meldingen, geen beheer |
| Vertrouwenspersoon | Optioneel; anders de centrale | Per afdeling of per instelling; anders de centrale |

### 3.5 Tenant & Billing context

**Aggregate: `Tenant`** — `TenantId`, `Name`, `Type` (Family/CareOrganization), `Plan` (Free/…), `Limits` (max. deelnemers, max. berichten per dag), `Status`.
Betaling (Stripe/Mollie) is buiten MVP; ontwerp wel een `Plan` en `Limits` zodat er later een `SubscriptionChanged` event kan komen.

---

## 4. CQRS: commands en queries

CQRS wordt toegepast als **scheiding van schrijf- en leesmodellen binnen één database** (geen event sourcing in MVP). Commands gaan via de domeinlaag en aggregates; queries lezen direct met Dapper/EF-projecties op read-geoptimaliseerde views.

### Commands

| Context | Command | Door |
|---|---|---|
| Conversation | `StartConversation(participantId)` | Deelnemer |
| Conversation | `SendParticipantMessage(conversationId, text)` → antwoord van kabouter | Deelnemer |
| Conversation | `CloseConversation(conversationId)` | Systeem (timeout) / deelnemer |
| Conversation | `SummarizeConversation(conversationId)` | Systeem (na `ConversationClosed`) |
| Conversation | `RemoveRememberedFact(participantId, factId)` | Ondersteuner met `CanManage` |
| Safeguarding | `RaiseConcern(...)` | Systeem |
| Safeguarding | `RouteAlert(concernId)` | Systeem (na `ConcernRaised`, via `AlertRoutingPolicy`) |
| Safeguarding | `EscalateAlert(alertId)` | Systeem (geen bevestiging binnen X uur) |
| Safeguarding | `AcknowledgeConcern(concernId)` | Ontvanger van de melding |
| Safeguarding | `ResolveConcern(concernId, note)` | Ontvanger van de melding |
| Household | `RegisterParticipant(name, careUnitId?, notes)` | Ondersteuner met `CanManage` / begeleider |
| Household | `SetParticipantLogin(participantId, pictureSequence)` | Ondersteuner met `CanManage` |
| Household | `AddSupportRelation(participantId, email, type, accessLevel, nameForParticipant)` | Ondersteuner met `CanManage` |
| Household | `RevokeSupportRelation(participantId, caregiverId)` | Ondersteuner met `CanManage` |
| Household | `CreateCareUnit(name)` / `AddStaffMember(careUnitId, email)` | Instellingsadmin |
| Household | `AppointConfidentialAdviser(scope, caregiverId)` | Instellingsadmin / platformbeheer |
| Household | `AssignGnome(participantId, gnomeId)` | Ondersteuner/systeem |
| Identity | `LoginParticipant(deviceToken, pictureSequence)` | Deelnemer |
| Tenant | `CreateTenant(name, type)` | Ondersteuner bij aanmelden |

### Queries

| Query | Voor |
|---|---|
| `GetParticipantHome(participantId)` → kabouter, avatar, begroeting | Deelnemer-client |
| `GetActiveConversation(participantId)` | Deelnemer-client |
| `ListMyParticipants(caregiverId)` | Ondersteuner-dashboard (over alle tenants heen) |
| `ListCareUnitParticipants(careUnitId)` | Begeleider |
| `GetConversationTranscript(conversationId)` (alleen bij `AccessLevel = FullTranscripts`) | Ondersteuner |
| `GetConversationSummaries(participantId, period)` | Ondersteuner (inzage zonder volledige tekst) |
| `ListOpenConcerns(caregiverId)` | Ondersteuner / vertrouwenspersoon |
| `GetConcernForAdviser(concernId)` | Vertrouwenspersoon (break-glass, gelogd) |
| `GetGnomeMemory(participantId)` | Ondersteuner (zien/aanpassen wat de kabouter onthoudt) |
| `ListGnomes()` | Ondersteuner / systeem |

### Inzage (beperkte toegang)

Inzage is het `AccessLevel` op de **steunrelatie** (per ondersteuner per deelnemer):

1. `AlertsOnly` — alleen meldingen bij zorgen, met een kort fragment.
2. `Summaries` — samenvattingen per gesprek en het geheugen van de kabouter.
3. `FullTranscripts` — volledige gesprekken.

Standaardwaarden in de MVP:

| Relatie | Standaard |
|---|---|
| Ouder in een gezin (`Guardian` + `CanManage`) | Summaries + meldingen |
| Begeleider via CareUnit (`Staff`) | Summaries + meldingen (instelbaar per CareUnit) |
| Ouder/voogd bij een instelling (`Guardian`) | Summaries + meldingen; FullTranscripts op verzoek |
| Vertrouwenspersoon | Alleen de melding en het betreffende gesprek, via break-glass |

Volledige transcripts zijn een bewuste keuze (privacy van de deelnemer; zie AVG). Let op: een volwassen deelnemer met een beperking heeft zelf ook privacyrechten. Wat een vertegenwoordiger mag inzien, hangt af van de vorm van vertegenwoordiging (curatele, mentorschap). Dat hoort bij het AVG-vraagstuk.

---

## 5. Het verloop van één bericht

```
Deelnemer-client                API                Conversation          Safeguarding         LLM
    │  spraak → tekst (device) │                     │                     │                │
    │──SendParticipantMessage──────►│                     │                     │                │
    │                          │──command──────────►│                     │                │
    │                          │                     │──1. assess input──►│ (regels +      │
    │                          │                     │◄──SafetyAssessment─│  classifier)   │
    │                          │                     │                     │                │
    │                          │      [High/Critical]: vast veilig script, ConcernRaised   │
    │                          │                     │                     │                │
    │                          │      [Safe/Low/Medium]:                   │                │
    │                          │                     │──2. compose prompt──────────────────►│
    │                          │                     │◄─────────────────── antwoord ───────│
    │                          │                     │──3. assess output─►│                │
    │                          │                     │◄── ok / herschrijf / fallback ──────│
    │                          │                     │──4. persist + events (outbox)       │
    │◄─────────── antwoord (tekst) ──────────────────│                     │                │
    │  tekst → spraak (device) │                     │                     │                │
```

Stappen:

1. **Input-check** (synchroon, snel): eerst deterministische regels (trefwoorden/zinsdelen in het Nederlands, met spellingvarianten zoals kinderen ze uitspreken: "dood gaan", "pijn doen", "niemand houdt van mij", "papa slaat"), daarna een classifier (moderatie-API van de LLM-aanbieder of een tweede LLM-call met een strikt JSON-schema).
2. **Prompt samenstellen**: persona + veiligheidsregels + herinnering + laatste K berichten.
3. **Output-check**: is het antwoord kort genoeg, kleutertaal, geen verboden inhoud, geen beloftes van geheimhouding, geen medisch advies? Zo niet: één keer opnieuw genereren, anders een generiek veilig antwoord.
4. **Opslaan + events** via een transactional outbox; `ConcernRaised` → `AlertRoutingPolicy` → melding naar ondersteuners of vertrouwenspersoon (asynchroon).

**LLM self-report**: de kabouter-prompt vraagt het model om naast het antwoord een veld `concern` terug te geven (structured output). Dat is een derde, extra signaal — nooit het enige.

---

## 6. Inloggen

| Wie | MVP | Later |
|---|---|---|
| **Ondersteuner / vertrouwenspersoon** | E-mail + wachtwoord of social login via een OIDC-provider (Keycloak lokaal; in productie Keycloak, Entra External ID, Auth0 of Zitadel). | Passkeys, MFA verplicht voor organisaties. |
| **Deelnemer** | **Apparaatkoppeling + plaatjes-pin**: een ondersteuner koppelt eenmalig een apparaat (QR-code of code). Daarna kiest de deelnemer zijn eigen foto/avatar en tikt een reeks van 3–4 plaatjes (bijv. 🍎🐶🚂). | NFC-kaartje/sticker, gezichtsherkenning van het apparaat (biometrie via OS), "altijd ingelogd" op een vertrouwd apparaat. |

Waarom plaatjes: kinderen kunnen geen cijfers lezen. Een cijfer-pincode kan wel als alternatief (voor wie dat wel kan).
Technisch: het device-token is een langlevend refresh token dat aan één `ParticipantId` en één apparaat gebonden is en door een ondersteuner kan worden ingetrokken. De plaatjes-pin is een tweede factor op dat apparaat, met rate limiting en lockout.

Ontwerp de login als een **strategie-patroon** (`IParticipantAuthenticationMethod`) zodat nieuwe manieren toevoegen een nieuwe klasse is, geen wijziging van bestaande code (Open/Closed).

---

## 7. Geheugen en samenvatten

Drie lagen:

1. **Korte termijn**: de laatste K (bijv. 10) berichten van het huidige gesprek gaan letterlijk mee in de prompt.
2. **Gesprekssamenvatting**: als een gesprek sluit (na X minuten inactiviteit of expliciet "dag kabouter"), maakt `SummarizeConversation` een korte samenvatting (3–5 zinnen) en extraheert **feiten** (namen van huisdieren, broertjes, lievelingseten, belangrijke gebeurtenissen: "gaat morgen naar de tandarts").
3. **Lange termijn**: `GnomeMemory` = een rollende samenvatting (max. ~300 woorden) + feitenlijst (max. ~50 feiten). Bij elke nieuwe gesprekssamenvatting wordt de rollende samenvatting opnieuw gecomprimeerd.

Regels:

- Zorgelijke inhoud (zelfbeschadiging e.d.) wordt **niet** in het geheugen opgenomen als feit; het staat in Safeguarding. De kabouter begint er niet zelf over.
- Een ondersteuner met `CanManage` kan feiten bekijken en verwijderen.
- "Terugkomen op" iets: de prompt bevat de feiten met datum, zodat de kabouter kan vragen "Hoe was het bij de tandarts?".
- Later (optioneel): vector search (pgvector) over oude gesprekken. Niet nodig voor MVP.

Samenvatten gebeurt asynchroon (achtergrondtaak via Wolverine), zodat het de chat niet vertraagt.

---

## 8. LLM-abstractie

```csharp
// Application layer — ports
public interface IChatCompletionService
{
    Task<ChatCompletionResult> CompleteAsync(ChatRequest request, CancellationToken ct);
}

public interface ISafetyClassifier
{
    Task<SafetyAssessment> AssessAsync(string text, SafetyContext context, CancellationToken ct);
}

public interface IConversationSummarizer
{
    Task<SummaryResult> SummarizeAsync(SummaryRequest request, CancellationToken ct);
}
```

- `ChatRequest` is een **eigen, provider-onafhankelijk model** (rollen, berichten, max tokens, temperatuur, gewenst JSON-schema).
- Implementatie bovenop **`Microsoft.Extensions.AI`** (`IChatClient`). Daarmee zijn Azure OpenAI, OpenAI, Anthropic, Ollama (lokaal!) en andere via adapters aan te sluiten zonder de applicatielaag te raken.
- Keuze per omgeving via configuratie: lokaal **Ollama** (gratis, offline, geen kinderdata naar buiten tijdens ontwikkelen), test/prod een hosted model.
- **Decorators** (SOLID: open/closed, single responsibility): `RetryingChatService`, `CachingChatService`, `MeteredChatService` (tokens/kosten per tenant), `TimeoutChatService`, `FallbackChatService` (tweede provider als de eerste faalt).
- Prompts staan **versiebeheerd als bestanden** in de repo (`prompts/gnomes/pim/v1.md`), niet in code. Het gebruikte prompt-versienummer wordt bij elk kabouter-bericht opgeslagen (traceerbaarheid).
- **Evaluatie**: een testset van voorbeeldvragen (incl. zorgwekkende) met verwachte eigenschappen van antwoorden draait in CI tegen de prompt (zie §11). Zo zie je of een nieuwe prompt of een ander model slechter wordt.

---

## 9. Clean Architecture en solution-structuur

Start als **modulaire monoliet**: één deploybare API, maar elke bounded context is een eigen module met eigen lagen. Modules praten alleen via publieke contracts (commands/queries/integration events), nooit via elkaars tabellen.

```
src/
  Babbelbos.Api/                         # ASP.NET Core host, endpoints, auth, DI-compositie
  Babbelbos.Worker/                      # (optioneel) aparte host voor achtergrondtaken
  Modules/
    Conversation/
      Conversation.Domain/               # aggregates, value objects, domain events — geen dependencies
      Conversation.Application/          # commands, queries, handlers, ports (interfaces)
      Conversation.Infrastructure/       # EF Core, LLM-adapters, repositories
      Conversation.Contracts/            # integration events + publieke DTO's
    Safeguarding/ (zelfde opbouw)
    GnomeCatalog/
    Household/
    Identity/
    Notifications/
    Tenancy/
  BuildingBlocks/
    BuildingBlocks.Domain/               # Entity, AggregateRoot, ValueObject, DomainEvent, Result
    BuildingBlocks.Application/          # ICommand/IQuery, pipeline behaviors (validation, logging, tx)
    BuildingBlocks.Infrastructure/       # outbox, tenant-resolutie, EF-basis
clients/
  web-participant/                       # PWA voor deelnemer (tablet/telefoon), grote knoppen, spraak
  web-caregiver/                         # dashboard voor ondersteuners en vertrouwenspersonen
prompts/                                 # persona's, veiligheidsregels, safety scripts
tests/
  *.Domain.Tests, *.Application.Tests, *.Integration.Tests (Testcontainers), Architecture.Tests, Prompt.Evals
deploy/
  docker/, helm/ of k8s/ (kustomize), terraform/ of bicep/
```

Afhankelijkheidsregels (afgedwongen met **NetArchTest** of **ArchUnitNET** in `Architecture.Tests`):

- `Domain` → niets.
- `Application` → `Domain`, `BuildingBlocks`.
- `Infrastructure` → `Application`, `Domain`.
- `Api` → alles, alleen voor compositie.
- Module A mag alleen `ModuleB.Contracts` refereren.

### Technologie

| Onderdeel | Keuze | Waarom |
|---|---|---|
| Runtime | .NET 10 (LTS), ASP.NET Core Minimal APIs | Actueel LTS |
| Messaging/mediator | **Wolverine** (in-process als mediator, en met transport voor async) | Één tool voor commands, events, outbox, retries, scheduled messages |
| Database | **PostgreSQL** | Gratis, overal beschikbaar (lokaal, Azure, AWS, alle andere providers), JSONB, pgvector voor later, Marten voor event sourcing later |
| ORM | EF Core (write side), Dapper of EF-projecties (read side) | |
| Message broker | MVP: Wolverine met **PostgreSQL-transport** (geen extra infra). Later: RabbitMQ of Azure Service Bus / Amazon SQS | Uitwisselbaar via Wolverine |
| Event sourcing (later) | **Marten** op dezelfde PostgreSQL | Past naadloos bij Wolverine |
| Auth | OIDC — Keycloak lokaal | Provider-neutraal |
| LLM | Microsoft.Extensions.AI; lokaal Ollama | Uitwisselbaar |
| Observability | OpenTelemetry → lokaal .NET Aspire dashboard of Grafana-stack; prod: provider naar keuze | Vendor-neutraal |
| Lokale dev-orkestratie | **.NET Aspire** voor snel F5-ontwikkelen + **kind/k3d** cluster voor k8s-oefening | |
| Clients | Eén **PWA** (React of Blazor) voor web/tablet/mobiel in MVP; later evt. .NET MAUI of React Native | PWA werkt op alle apparaten, met spraak via de browser |
| Validatie | FluentValidation in Wolverine-middleware | |

---

## 10. Multi-tenancy

- **Model**: gedeelde database, gedeeld schema, kolom `TenantId` op elke tenant-gebonden tabel.
- **Afdwingen** op drie niveaus:
  1. Tenant-resolutie uit het token (claim `tenant_id`) in middleware → `ITenantContext`.
  2. EF Core global query filters op `TenantId`.
  3. **PostgreSQL Row-Level Security** als vangnet (`SET app.tenant_id` per connectie).
- Elke module heeft een eigen **database-schema** (`conversation`, `safeguarding`, …), wat modulegrenzen zichtbaar maakt en later afsplitsen makkelijk maakt.
- Later: grote zorgorganisaties eventueel een eigen database (Wolverine/Marten ondersteunen database-per-tenant).
- Limieten per tenant (berichten per dag, aantal deelnemers) via `Tenant.Limits` + rate limiting middleware; tokenverbruik per tenant meten voor latere facturering.

---

## 11. Deployment, CI/CD en omgevingen

### Lokaal

- `docker compose up` of **Aspire AppHost**: API, Worker, PostgreSQL, Keycloak, Ollama, Mailpit (nep-mailserver), Aspire dashboard.
- Daarnaast een **k3d/kind**-cluster met dezelfde Helm-chart als productie (oefening met k8s, ingress, secrets).

### Omgevingen

`local` → `dev` (elke merge naar main) → `staging` → `prod` (handmatige goedkeuring of tag).

### CI-pipeline (GitHub Actions, bij elke PR)

1. `dotnet build` met warnings-as-errors, `dotnet format --verify-no-changes`
2. Unit- en architectuurtests
3. Integratietests met **Testcontainers** (PostgreSQL)
4. **Prompt-evals** (met Ollama in CI of een goedkoop hosted model; zie §8) — vooral de veiligheidscases moeten 100% slagen
5. Security: dependency-scan (`dotnet list package --vulnerable`, Dependabot), SAST (CodeQL), container-scan (Trivy), secret-scan
6. Docker images bouwen (multi-stage, chiseled/distroless images), taggen met commit-SHA
7. Frontend lint/test/build

### CD-pipeline

- Image push naar container registry (GHCR — provider-neutraal).
- **GitOps** met Argo CD of Flux: de pipeline werkt de image-tag bij in een `deploy`-map/repo; het cluster haalt de wijziging zelf op.
- Database-migraties als aparte k8s Job (EF Core migration bundle) vóór de nieuwe versie start.
- Semantische versies en changelog automatisch via Conventional Commits + release-please.
- Rollback = vorige tag terugzetten in Git.

### Cloud-neutraal

- Infrastructure as Code met **OpenTofu/Terraform**, met per provider een module: Azure (AKS + Azure Database for PostgreSQL), AWS (EKS + RDS), of goedkoop: Hetzner/Scaleway/DigitalOcean managed Kubernetes + managed Postgres.
- Advies voor een leerproject met lage kosten: start met één kleine managed Kubernetes bij een goedkope EU-provider, of Azure Container Apps als tussenstap. **Houd data in de EU** (AVG).

### Observability en beheer

- OpenTelemetry traces/metrics/logs; dashboards voor: responstijd kabouter, LLM-latency, tokens/kosten per tenant, aantal zorgsignalen.
- **Geen gesprekstekst in logs** — alleen ID's.
- Health checks (`/health/live`, `/health/ready`), alerts op foutpercentage en op "melding niet afgeleverd".

---

## 12. Privacy en AVG (open punt — TBD)

Nog uit te werken, maar het ontwerp houdt hier al rekening mee:

- Bijzondere categorie: gegevens van kinderen en mogelijk gezondheidsgegevens (beperking, mentaal welzijn). Een **DPIA** is vrijwel zeker verplicht.
- Toestemming van ouder/wettelijk vertegenwoordiger bij registratie.
- Dataminimalisatie: geen achternaam, geen geboortedatum (alleen jaar, optioneel), geen foto's van de deelnemer.
- Bewaartermijnen: ruwe berichten bijv. 90 dagen, daarna alleen samenvattingen (configureerbaar per tenant).
- Recht op inzage en verwijdering: export- en verwijder-command per deelnemer.
- Verwerkersovereenkomst met LLM-aanbieder; kies een aanbieder/regio die **niet traint op de data** en EU-dataresidentie biedt.
- Versleuteling at rest (database) en in transit (TLS); overweeg kolomversleuteling voor berichttekst.
- Openstaande vragen: wie is verwerkingsverantwoordelijke bij zorgorganisaties? Mag de kabouter "geheimen" bewaren tegenover ondersteuners? (Advies: nee — de kabouter belooft nooit geheimhouding.)

---

## 13. MVP-scope tegenover later

| Onderwerp | MVP | Later |
|---|---|---|
| Kabouters | 1 kabouter ("Pim"), geseed | Meerdere, systeem stelt voor, tenant-eigen kabouters |
| Deelnemer-UI | PWA: grote praatknop, spraak via browser/OS, voorlezen, avatar | Server-side STT/TTS met eigen kabouterstem, animaties, offline-modus |
| Inloggen deelnemer | Apparaatkoppeling + plaatjes-pin | NFC, biometrie, meerdere deelnemers op één tablet (woning) |
| Ondersteuners | OIDC-login, deelnemers beheren, steunrelaties (Guardian/Staff), samenvattingen, meldingen per e-mail, vertrouwenspersoon (tenant + centraal) | CareUnits en begeleiders van instellingen uitgebreid, push-meldingen, app, escalatie bij geen bevestiging |
| Veiligheid | Regels + classifier + vaste scripts + directe melding | Menselijke review-wachtrij, escalatie naar hulplijn-info (Kindertelefoon 0800-0432, Veilig Thuis 0800-2000) voor ondersteuners |
| Geheugen | Samenvatting + feiten | pgvector, semantisch zoeken |
| Data | PostgreSQL, CRUD + CQRS-scheiding | Event sourcing met Marten voor Conversation |
| Messaging | Wolverine in-process + PostgreSQL-outbox | RabbitMQ / Service Bus, losse services |
| Multi-tenant | TenantId + RLS | Database per tenant voor grote klanten |
| Betaling | Plan-veld, limieten | Stripe/Mollie, facturatie op tokengebruik |
| Deployment | Docker, Aspire, k3d, GitHub Actions, één cloudomgeving | Meerdere omgevingen, GitOps, blue/green |

### Voorgestelde volgorde (iteraties)

1. **Walking skeleton**: repo, solution-structuur, CI, Docker, `/health`, deploy naar één omgeving. (Eerst de pijplijn, dan de features.)
2. Deelnemer kan praten met de kabouter (tekst in, tekst uit) met Ollama lokaal; berichten opgeslagen.
3. Veiligheid: input/output-check, vaste scripts, `Concern` + e-mailmelding.
4. Ondersteuner: login, deelnemer registreren, steunrelaties, apparaat koppelen, plaatjes-pin.
   4b. Instelling: CareUnits, begeleiders, vertrouwenspersoon en de `AlertRoutingPolicy`.
5. Spraak in de deelnemer-client (browser STT/TTS), avatar.
6. Geheugen: samenvatten en feiten.
7. Multi-tenancy hard maken (RLS), inzage-niveaus.
8. Productie: IaC, staging/prod, observability, prompt-evals in CI.

---

## 14. Nog benodigde informatie / open vragen

1. **AVG**: wie is verwerkingsverantwoordelijke, bewaartermijnen, DPIA (zie §12).
2. **Taal**: alleen Nederlands, of ook andere talen (bijv. voor kinderen die thuis een andere taal spreken)?
3. **Tijdsgrenzen**: mogen ondersteuners schermtijd/tijdvensters instellen ("kabouter slaapt na 19:00")?
4. **Vertrouwenspersoon** (deels opgelost, zie §3.2 `AlertRoutingPolicy`): meldingen over misbruik of verwaarlozing gaan naar de vertrouwenspersoon van de afdeling of instelling, en anders naar de centrale vertrouwenspersoon. Nog open:
   - Wie is de centrale vertrouwenspersoon in productie, en wat zijn de reactietijden? Hoe zorg je dat het platform niet als hulpdienst wordt gezien?
   - Moet de app voldoen aan de Wet meldcode huiselijk geweld en kindermishandeling (geldt voor professionals, dus voor instellingen)? Stem het meldproces af met een deskundige.
   - Mag een ondersteuner zien dát er een melding bij de vertrouwenspersoon ligt? (Huidig voorstel: ja, zonder inhoud. Mogelijk is zelfs dat te veel informatie.)
   - Wat als de deelnemer een tweede, onterechte melding doet (bijv. uit boosheid)? De vertrouwenspersoon moet dit kunnen afsluiten zonder dat de ondersteuner wordt belast.
5. **Budget**: maximale maandelijkse kosten voor LLM en hosting (bepaalt modelkeuze).
6. **Doelgroep met beperking**: zijn er deelnemers die ook niet (verstaanbaar) kunnen praten? Dan is een beeld-/pictogram-invoer (zoals bij ondersteunde communicatie, bijv. pictogrammen of Sclera-picto's) nodig.
7. **Kabouteruiterlijk/stem**: wie maakt de illustraties en kiest de stem?
8. **Vertegenwoordiging van volwassen deelnemers**: welke inzage heeft een curator of mentor, en hoe registreert een instelling die vorm van vertegenwoordiging?

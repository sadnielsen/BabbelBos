# Prompts

Prompts staan als bestanden in de repo, met een versie in de bestandsnaam. **Wijzig nooit een bestaande versie**: maak een nieuwe (`v2`) en zet de versie om in `gnome.json`. Bij elk kabouterbericht wordt de gebruikte versie opgeslagen.

| Bestand | Inhoud |
|---|---|
| `gnomes/<id>/gnome.json` | Naam van de kabouter, of het de standaardkabouter is, en welke versies gelden |
| `gnomes/<id>/persona.<versie>.md` | Persoonlijkheid en manier van praten |
| `gnomes/<id>/safety-scripts.<versie>.json` | Vaste antwoorden bij High/Critical en de `Fallback` bij een technische fout (geen LLM) |
| `shared/safety-rules.v1.md` | Veiligheidsregels die voor élke kabouter gelden |
| `shared/context.v1.md` | Context over de deelnemer (naam, vertrouwde volwassenen, geheugen) |
| `shared/output-format.v1.md` | JSON-uitvoer en voorbeelden |

De systeemprompt is: persona + veiligheidsregels + context + uitvoerformaat. Velden als `{{participant_name}}` vult de `PromptComposer` in; een veld zonder waarde wordt "(nog niets bekend)".

De ontwerpversie met toelichting staat in [docs/02-kabouter-system-prompt.md](../docs/02-kabouter-system-prompt.md).

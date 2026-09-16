# Librus

En boklåningsapp ur låntagarens perspektiv. C#/.NET med EF Core och Postgres i backend, Next.js i frontend.

## Kör lokalt

Kräver Docker, .NET SDK 10 och Node 20+.

```
./scripts/dev.sh
```

Migrationer och seeddata körs vid uppstart, så första körningen tar några sekunder extra.

|                   |                                 |
| ----------------- | ------------------------------- |
| Frontend          | http://localhost:3000           |
| API               | http://localhost:5092           |
| API-dokumentation | http://localhost:5092/scalar/v1 |

Nollställ databasen med `docker compose down -v` och kör skriptet igen.

Tester: `cd backend && dotnet test`. De kör mot `librus_test`, som skapas när docker-volymen initieras första gången.

## Upplägg

Ett backendprojekt med mappgränser i stället för separata assemblies. På den här storleken hade fler projekt kostat mer i läsbarhet än de gett i struktur. Domänen känner ändå inte till HTTP: `LoanService` tar vanliga parametrar och kastar domänundantag som API-lagret översätter till statuskoder. Inga repositories ovanpå EF Core, `DbContext` gör redan det jobbet.

## Låneflödet

Den del jag lagt mest tid på.

Ett exemplar kan inte ha två aktiva lån samtidigt, och det är databasen som håller regeln:

```sql
CREATE UNIQUE INDEX ix_loans_active_copy
  ON loans (copy_id) WHERE returned_at IS NULL;
```

`LoanService` kollar tillgänglighet före skrivning för att kunna ge ett begripligt felmeddelande, men litar inte på kontrollen. Hinner någon emellan fångas unique-violationen och nästa lediga exemplar prövas. Utan indexet hade det krävt en transaktion med radlås.

Låna tar `bookId`, inte `copyId`. Låntagaren väljer bok, biblioteket väljer exemplar.

Reglerna ligger samlade i `LoanPolicy`:

- 28 dagars lånetid, max 5 aktiva lån
- Max 2 förlängningar om 7 dagar, räknade från förfallodatumet
- Försenade lån kan inte förlängas, då hade det nya förfallodatumet kunnat hamna i det förflutna
- Försenade lån blockerar nya lån
- Samma bok kan inte lånas två gånger samtidigt

`GET /api/me` svarar om låntagaren får låna och annars varför. Låneknappen läser det svaret i stället för att räkna ut reglerna själv, så användaren får samma besked som ett lånförsök hade gett.

## Datamodell

Exemplar är egna rader, inte en räknare på boken. Uppgiften frågar efter hur många exemplar som är lediga respektive utlånade, och då behöver exemplaret vara något ett lån kan peka på. Raderna skulle i teorin kunna motsvara ett fysiskt exemplar av boken.

Lånet når boken via `copy_id`, vilket kostar en join i topplistan och rekommendationerna. Att dubbellagra `book_id` på `loans` hade tagit bort joinen, men exemplaret är sanningskällan och jag har inget mätvärde som motiverar det.

## Uppskattad lästid

Bygger på vad låntagare rapporterat att de läst i minuter, inte på hur länge lånet varade. Lånetiden säger när boken lämnades tillbaka, inte hur länge den lästes. Jag använder medianen för att enstaka extremvärden annars drar iväg siffran, och vid färre än fem rapporter bibliotekets lästakt per sida gånger sidantalet.

API:t säger vilken av källorna som använts och UI:t visar det. "Baserat på 14 rapporter" går att väga in, samma siffra utan sammanhang ser bara exakt ut.

## SQL

Lästid, topplista och rekommendationer är aggregeringar och ligger som SQL. Frågorna blir lättare att läsa så, och `percentile_cont` finns inte i LINQ. Bokdata hämtas i en andra fråga, att trycka in titlar och tillgänglighet i samma SQL hade gett frågor ingen vill underhålla.

## Frontend

Hämtning i server components, mutationer i klientkomponenter följt av `router.refresh()`. TanStack Query hade lagt en andra cache ovanpå den App Router redan har. Alla anrop går med `no-store`, eftersom lånestatus ändras av användarens egna klick.

Sökning, paginering och sidstorlek ligger i URL:en. Det ger delbara länkar och en bakåtknapp som fungerar.

Fel kommer tillbaka som `ProblemDetails` och `detail`-texten visas som den är, så felmeddelandena skrivs en gång och bara i domänen.

## Seeddata

Böckerna kommer från Open Library och ligger som `books.json` i repot, så seedningen behöver inget nätverk. Omslagen hämtas från `covers.openlibrary.org` i webbläsaren och kräver internet för att synas.

Lånehistoriken genereras relativt dagens datum. Med fasta datum hade "senaste månaden" i topplistan varit tom för alla som körde appen senare.

Lånen byggs som en kedja per exemplar, ett i taget framåt i tiden, så seeddatan kan inte bryta mot det unika indexet. Varje låntagare har två favoritgenrer och lånar oftare inom dem, annars blir lånemönstret brus och rekommendationerna säger ingenting.

Genrerna kommer från Open Librarys ämnesindex och blir ibland oväntade. Enstaka romaner har hamnat under Populärvetenskap.

## Tester

12 tester mot låneflödet, mot riktig Postgres och inte EF Cores InMemory-provider. Det viktigaste skyddet finns bara i databasen, och InMemory hade gett gröna tester för kod som går sönder i drift. Det viktigaste testet startar två samtidiga lån på bokens enda exemplar och kontrollerar att ett överlever.

`IClock` finns för att förseningar och förlängningar ska gå att testa utan att vänta 28 dagar.

## Autentisering

`ICurrentUser` läser `X-User-Id` och faller tillbaka på låntagare 1. Ingen klient sätter headern i dag, så identiteten är obekräftad.

## Medvetna avgränsningar

Uppgiften är satt till 3-4 timmar och säger att ett genomtänkt låneflöde väger tyngre än många halvfärdiga funktioner. Bortprioriterat:

- Genomarbetad design
- Laddindikatorer vid sökning, paginering och bildhämtning
- Responsivitet på mindre skärmar
- Betyg visas men kan inte sättas, och egna recensioner kan inte skrivas
- Svenskt gränssnitt med engelska boktitlar
- Utförlig endpoint-dokumentation i Scalar
- Profilvy

Två saker jag vet är ofullständiga:

Det unika indexet skyddar exemplaret. Lånetaket och regeln om samma bok kontrolleras i kod utan transaktion, så två samtidiga anrop från samma låntagare kan passera båda. Den regel som kan ge trasig data i databasen är den jag lade i databasen, resten hade jag tagit med en transaktion om det gått till drift.

Lästiden visas bara i detaljvyn. I listan hade den krävt en fönsterfunktion i samma fråga eller en query per rad, och siffran behövs där beslutet fattas.

## Med mer tid

- TypeScript-typer genererade från OpenAPI-schemat i stället för handskrivna
- Uppslagningen av bokdata efter SQL ligger på två ställen och borde brytas ut
- Testcontainers i stället för en delad testdatabas
- Paginerade recensioner
- Nyförvärv som egen lista. Populära böcker dominerar oavsett tidsfönster eftersom katalogen är statisk

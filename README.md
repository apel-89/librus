## Kör lokalt

Kräver Docker, .NET SDK 10 och Node 20+.

    ./scripts/dev.sh

Startar Postgres, migrerar, seedar och kör både API och frontend.
Nollställ databasen med `docker compose down -v` och kör skriptet igen.

Eftersom tiden för detta projekt har varit begränsad, och det faktum att mycket funktionalitet ska ingå, har vissa saker prioriterats bort för att gynna flödet mellan backend och frontend.# Librus

En boklåningsapp ur låntagarens perspektiv. Backend i C#/.NET med Entity Framework Core och Postgres, frontend i Next.js.

## Kör lokalt

Kräver Docker, .NET SDK 10 och Node 20+.

```
./scripts/dev.sh
```

Skriptet startar Postgres, installerar beroenden och kör både API och frontend. Migrationer och seeddata körs automatiskt av API:t vid uppstart i Development, så första körningen tar några sekunder extra.

| | |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:5092 |
| API-dokumentation | http://localhost:5092/scalar/v1 |

Nollställ databasen med `docker compose down -v` och kör skriptet igen.

Tester: `cd backend && dotnet test`. De körs mot databasen `librus_test`, som skapas automatiskt i samma container.

## Struktur

```
backend/src/Librus.Api/
├── Domain/       entiteter, domänregler, domänundantag
├── Data/         DbContext, migrationer, seedning
├── Features/     endpoints och services per område
└── Sql/          inbäddade SQL-frågor
web/src/
├── app/          routes
├── components/
└── lib/          API-klient och hjälpfunktioner
```

Jag har medvetet hållit backenden i ett projekt med tydliga mappgränser i stället för Clean Architecture med separata assemblies. På den här storleken tillför assembly-gränser ingenting, och de hade gjort lösningen dyrare att läsa. Domänlagret känner däremot inte till HTTP: `LoanService` tar enkla parametrar och kastar domänundantag, som API-lagret översätter till statuskoder.

Av samma skäl finns inga repository-abstraktioner över EF Core. `DbContext` är redan Unit of Work och repository; att kapsla in den hade lagt till ett lager utan att lösa något.

## Datamodell

```
books ──< book_copies ──< loans >── users
  │
  ├──> authors
  ├──> genres
  └──< feedback >── users
```

Exemplar modelleras explicit i stället för som en räknare på boken. Uppgiftens tillgänglighetsbegrepp, hur många exemplar som är lediga respektive utlånade, förutsätter att exemplaret finns som entitet, och det gör att ett lån kan knytas till ett fysiskt exemplar precis som på ett riktigt bibliotek.

Lånet når boken via `copy_id → book_id`, vilket kostar en join i topplistan och rekommendationerna. Alternativet hade varit att denormalisera `book_id` på `loans`, men exemplaret är sanningskällan för vilken bok lånet gäller, och duplicering hade varit en prestandaoptimering utan mätning bakom sig.

## Låneflödet

Det här är den del jag lagt mest omsorg på.

**Ett exemplar kan aldrig ha två aktiva lån.** Garantin ligger i databasen, inte i koden:

```sql
CREATE UNIQUE INDEX ix_loans_active_copy
  ON loans (copy_id) WHERE returned_at IS NULL;
```

`LoanService` kontrollerar tillgänglighet före skrivning för att kunna ge ett begripligt felmeddelande i normalfallet, men låter databasen ha sista ordet. Hinner någon annan låna exemplaret däremellan fångas unique-violation och nästa lediga exemplar prövas. Utan det partiella indexet hade flödet krävt en transaktion med radlås.

**Låna tar `bookId`, inte `copyId`.** Låntagaren väljer bok, biblioteket allokerar exemplar.

**Domänregler** (samlade i `LoanPolicy`):

- Lånetid 28 dagar, max 5 aktiva lån per låntagare
- Max 2 förlängningar, räknat från förfallodatumet
- Försenade lån kan inte förlängas utan måste lämnas tillbaka; annars hade det nya förfallodatumet kunnat hamna i det förflutna
- Försenade lån blockerar nya lån
- Samma bok kan inte lånas två gånger samtidigt

`GET /api/me` returnerar om låntagaren får låna och i så fall inte varför, som ett enum. Frontenden behöver därmed inte känna till reglerna, och felmeddelandet i UI:t kommer från samma källa som det fel ett lånförsök faktiskt hade gett.

## Uppskattad lästid

Baseras på låntagarnas egenrapporterade lästid i minuter, inte på lånetiden. Lånetid mäter när boken lämnades tillbaka, inte hur länge den lästes.

Uppskattningen är **medianen**, inte medelvärdet, eftersom enstaka extremvärden annars drar iväg siffran. Har boken färre än fem rapporter används i stället bibliotekets globala lästakt per sida gånger bokens sidantal.

API:t returnerar vilken av de två källorna som använts, och UI:t visar det. En siffra som säger "baserat på 14 rapporter" är beslutsstöd; samma siffra utan sammanhang är en gissning som ser exakt ut.

## Rå SQL

Tre frågor ligger som inbäddade `.sql`-filer i stället för LINQ:

- **Lästid** — Postgres `percentile_cont` för medianberäkning
- **Topplista** — aggregering över lånehistoriken med valbar tidsperiod (senaste månaden, senaste året, all time via samma fråga)
- **Rekommendationer** — co-occurrence: låntagare som lånat boken, deras övriga böcker, sorterat på antal delade låntagare

Alla tre är aggregeringar där SQL är tydligare än LINQ. Bokdata hämtas sedan med EF i en andra fråga, att pressa in titlar, författare och tillgänglighet i samma SQL hade gett frågor ingen vill läsa.

EF:s query filter för soft delete gäller inte i rå SQL, så `deleted_at IS NULL` står explicit där det behövs.

## Frontend

Datahämtning sker i server components. TanStack Query, som jag annars använder dagligen, valdes bort eftersom det hade infört ett andra cachelager parallellt med App Routers utan att lösa något som inte redan var löst. Mutationer sker i klientkomponenter följt av `router.refresh()`.

Sökning, paginering och sidstorlek ligger i URL:en i stället för i React-state. Det ger delbara länkar, fungerande bakåtknapp och automatisk prefetch av nästa sida.

API-fel kommer tillbaka som `ProblemDetails` och `detail`-texten visas direkt i UI:t. Felmeddelandena är därmed skrivna en gång, i domänen.

## Seeddata

Böckerna är hämtade från Open Library och ligger som `books.json` i repot. Seedningen behöver aldrig nätverk. Omslagsbilder hämtas däremot från `covers.openlibrary.org` i webbläsaren, så de kräver internetanslutning för att visas.

Lånehistoriken genereras däremot vid seedning, relativt aktuell tid. Med statiska datum hade "senaste månaden" i topplistan varit tom den dag någon annan körde appen.

Två detaljer som gör datan användbar:

**Lånen byggs som en kedja per exemplar** — ett lån i taget framåt i tiden med hylltid emellan. Det gör att seeddatan strukturellt inte kan bryta mot det unika indexet.

**Varje låntagare har två favoritgenrer** och lånar fyra gånger oftare inom dem. Utan den strukturen blir lånemönstret brus och rekommendationerna meningslösa.

Genrerna kommer från Open Librarys ämnesindex och är ibland oväntade, enstaka romaner har hamnat under Populärvetenskap.

## Tester

13 tester mot låneflödet, körda mot riktig Postgres och inte mot EF Cores InMemory-provider. Anledningen är att lösningens viktigaste garanti, det partiella unika indexet, bara existerar i databasen. InMemory hade rapporterat gröna tester för kod som går sönder i drift.

Det viktigaste testet startar två samtidiga lån på bokens enda exemplar och verifierar att exakt ett överlever.

`IClock` finns för att göra tidsberoende regler testbara. Utan den går förseningar och förlängningar inte att testa utan att vänta 28 dagar.

## Autentisering

Appen antar en inloggad låntagare. Aktuell användare läses från headern `X-User-Id`, med fallback till låntagare 1. Sömmen ser ut som den hade gjort med riktig autentisering, men identiteten är obekräftad.

## Medvetna avgränsningar

Uppgiften är satt till 3–4 timmar och anger att ett genomtänkt låneflöde väger tyngre än många halvfärdiga features. Följande har därför prioriterats bort:

- Genomarbetad design
- Laddindikatorer vid sökning, paginering och bildhämtning
- Responsivitet på mindre skärmar
- Stjärnbetyg som inmatning (betyg visas, men kan inte sättas)
- Språkstöd: gränssnittet är på svenska medan boktitlarna är på engelska
- Utförlig endpoint-dokumentation i Scalar
- Profilvy får användaren
- Möjlighet att skriva egen recension

Lästidsuppskattningen visas bara i detaljvyn. I listvyn hade den krävt antingen en fönsterfunktion i samma fråga eller en query per rad, och beslutsstödet behövs där man faktiskt fattar beslutet.

## Med mer tid

- TypeScript-typer genererade från OpenAPI-schemat i stället för handskrivna
- Uppslagningen av bokdata efter rå SQL finns på två ställen och borde brytas ut
- Testcontainers i stället för en delad testdatabas
- Paginerade recensioner
- Nyförvärv som egen lista, populära böcker dominerar oavsett tidsfönster, eftersom katalogen är statisk

Bortprioriterade features:

- Design för hur böcker listas i tabellen
- Laddindikator vid sökning/paginering/bildhämtning
- Responsivitet på mindre skärmar
- Stjärnbetygssystem
- Språkstöd (medveten om inkonsekvensen med svensk sida och engelska boktitlar)
- Genomarbetad dokumentation över api-endpoints i Scalar
- Smart lösning för API-hantering och caching i frontend
- Profilvy för användare

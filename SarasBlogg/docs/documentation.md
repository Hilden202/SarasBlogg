# SarasBlogg – Dokumentation

## Historik
- Projektet startade på Azure med SQL Server via studentlicens.
- När Azure Student upphörde och krediterna tog slut flyttades projektet till Render med PostgreSQL.
- Ursprunglig bildhantering sparade endast en bild per inlägg i databasen, och den ersattes automatiskt vid ny uppladdning.
- Målet med API-utbrytningen är att göra frontenden helt API-driven för att även kunna användas av appar och andra klienter.

---

## Teknisk översikt (uppdaterad)
**Teknik:** .NET Razor Pages (C#), Entity Framework Core, PostgreSQL (tidigare SQL Server)  
**Frontend:** Bootstrap + anpassad CSS  
**Backend:** Razor Pages + separat API-projekt (SarasBloggAPI)

---

## Infrastruktur
- Frontend och API körs på Render (tidigare Azure)
- PostgreSQL-databas (Render)
- GitHub för versionshantering med branches
- Deployment: Render (GitHub Actions kan läggas till senare)
- Kommentarhantering: AI-analys via Google Perspective API + regex
- Hosting-alternativ: Flytt av frontend till GitHub Pages är planerad (API driver all funktionalitet)
- **Bilder**
  - Statiska bilder (loggor, bakgrunder, ramar) ligger i frontend eller separat repo (sarasblogg-media via GitHub Pages)
  - Dynamiska bilder hanteras av SarasBloggAPI och lagras i sarasblogg-media på GitHub
- Forwarded headers aktiverat för korrekt proxy-drift på Render
- Health endpoints för driftövervakning (allmänt och DB)
- SendGrid används för transaktionsmail (bekräftelse/återställning)
  - Dev-läge: loggar e-post till konsolen
  - Prod-läge: skickar riktiga mail
- API:t kan köras i två lokala miljöer:  
  - **Test** – använder en separat Docker-databas (`sarasblogg_test`) för helt isolerad testning.  
    Här är det möjligt att köra automatiserade end-to-end-tester med **Playwright** utan risk att påverka produktionen.  
  - **Prod** – kör samma kod och konfiguration som den skarpa Render-miljön, men lokalt på datorn (använder riktiga databasen om den är vald).  
    Detta gör att man kan växla mellan sandbox och skarp miljö utan att ändra klientkod.

---

## API-klienter
- Alla `*APIManager` (Blogg, BloggImage, Comment, ForbiddenWord, AboutMe, ContactMe, Like) är typed HttpClient via `AddHttpClient` i `Program.cs`.
  - Gemensam konfiguration: BaseAddress, Timeout
  - Robusthet: Polly-retry för kallstart/temporära fel
  - JSON-hantering med `System.Text.Json` (standardinställningar)
- `UserAPIManager` är också typed HttpClient
  - Endast GET/HEAD har retry (POST har ingen retry för att undvika dubletter)
	- **Backup & Restore**
  - Daglig backup körs via schemalagd PowerShell (`backup-sarasblogg.ps1`) som använder `pg_dump`.
  - Scriptet sparar dumpfiler i `Desktop\SarasBlogg\Backups` och loggar i CSV/XLSX.
  - Viktigt: den schemalagda uppgiften måste köra **.ps1 direkt i PowerShell** (inte via `.bat`), annars kan datum i filnamnet bli fel p.g.a. Windows `%DATE%`-format.
  - Återställning sker i pgAdmin med **Clean before restore** ibockat för att undvika FK-krockar.

---

## Funktioner
**Besökare**
- Läsa blogginlägg
- Lämna kommentarer (AI + regex-filtrering via API)

**Admin**
- Skapa, redigera, arkivera/dölja blogginlägg
- Hantera kommentarer
- Hantera kontaktmeddelanden
- Rollbaserad åtkomstkontroll
- Raderingsknapp för användare i rollhanteringsvyn
- Rollistan i admin-vyn sorteras i fast ordning (user, superuser, admin, superadmin)

---

## Bildhantering
- Bilder sparas i både GitHub och databasen via `GitHubFileHelper`
- `Order`-kolumn i DB styr bildordning
- `AddImageAsync` sätter automatiskt nästa lediga `Order`
- **API**
  - `GET /api/BloggImage/blogg/{bloggId}` → hämtar bilder
  - `PUT /api/BloggImage/blogg/{bloggId}/order` → uppdaterar ordning
- Byt omslagsbild genom klick i admin
- Radering av bilder fungerar även för första bilden
- List- och detaljvyer hämtar bilder via API
- `GetAllBloggsAsync()` fyller alltid på med bilder
- Startsidan visar inte arkiverade inlägg
- `Blogg.Image` ersatt av `BloggWithImage` (lista av `BloggImageDto`; ibland `FirstImage`)
- `Admin/Index` laddar bilder via `LoadBloggsWithImagesAsync()` → `BloggImageAPIManager.GetImagesByBloggIdAsync()`
- AboutMe-bild följer samma API-upplägg
- Bild-URL-hantering centraliserad, debugkod städad
- Radering av blogg tar även bort tillhörande bilder via API

---

## Användarhantering
- Roller: User, Superuser, Admin, Superadmin
- Inloggning kräver bekräftad e-post
- Hela Identity-flödet är nu API-drivet i klienten (login, register, profil, ändra e-post/lösenord, radera konto)
- Klienten använder cookie-auth + JWT i headern för API-anrop
- **Skydd för systemanvändare `admin@sarasblogg.se`:**
  - Kan inte raderas (API & frontend)
  - Kan inte få roller borttagna
  - Roller visas som låsta “Ja” i admin-UI
- **Notiser vid nya blogginlägg**
  - Användare kan ha `NotifyOnNewPost = true` (lagras på ApplicationUser)

---

## Auth / API
**Endpoints (urval)**
- `POST /api/auth/register` – skapar konto, roll: User
- `POST /api/auth/confirm-email` – bekräftar e-post
- `POST /api/auth/login` – returnerar JWT (kräver bekräftad e-post)
- `POST /api/auth/logout` – loggar ut (cookie-scenario)
- `GET /api/users/me` – utökad info: Id, UserName, Email, Name, BirthYear, PhoneNumber, EmailConfirmed, Roles
- `POST /api/auth/resend-confirmation` – skickar ny bekräftelselänk
- `POST /api/auth/forgot-password` – skickar återställningslänk
- `POST /api/auth/reset-password` – återställer lösenord
- `POST /api/users/me/change-password` – byter lösenord
- `POST /api/users/me/set-password` – sätter lösenord om saknas
- `POST /api/users/me/change-email/start` + `POST /api/users/change-email/confirm` – byte av e-post
- `PUT /api/users/me/profile` – uppdaterar profil (Name, BirthYear, PhoneNumber)
- `GET /api/users/me/personal-data` – hämtar personlig data, roller och claims
- `DELETE /api/users/me` – raderar konto

### API-säkerhet (RBAC – kort)
- JWT med roll-claims (`ClaimTypes.Role`), roller skrivs i **gemener** i token.
- Policies: `RequireUser`, `CanModerateComments`, `CanManageBlogs`, `AdminOrSuperadmin`, `SuperadminOnly`.
- **Publikt läs**: Blogg & BloggImage (GET), Kommentarer (GET), Likes (GET).
- **Skriv låst**: Blogg/BloggImage kräver `CanManageBlogs`; ForbiddenWord (**GET/POST/DELETE**) & ContactMe (**GET/DELETE**) kräver `AdminOrSuperadmin`.
- **Rollhantering**: hela `RoleController` är `SuperadminOnly`. Grundroller (`user`, `superuser`, `admin`, `superadmin`) kan **inte** tas bort.
- **Kommentarer**: POST är öppen (`AllowAnonymous`); borttag kräver ägarskap eller `CanModerateComments` (massradering per blogg kräver `CanModerateComments`).

**Klientens auth-beteende**
- Cookie-schema: `SarasAuth`
- Vid login:
  - API returnerar JWT (access/refresh)
  - JWT lagras i `IAccessTokenStore` + HttpOnly-cookie `api_access_token`
  - Klienten skapar auth-cookie (`SarasAuth`) med claims från JWT
  - `JwtAuthHandler` bifogar `Authorization: Bearer …` till alla API-anrop
- Vid logout / radera konto:
  - `SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`
  - `HttpContext.User` nollställs
  - Cookies (`SarasAuth`, `api_access_token`) tas bort
  - `IAccessTokenStore` rensas

---

## Design
- Responsiv layout
- Sidor: Index, Home, AboutMe, Archive, Contact, Admin
- Footer: sociala ikoner + länk till integritetspolicy
- **Metadata & delning**
  - OG-taggar i `_Layout.cshtml` (`og:title`, `og:description`, `og:image`, `og:url`, `og:type`)
  - `og:image:type`, `twitter:card`
  - Meta-bild: `jagmedbarnenmeta.png` (1200×630) i `wwwroot/img/logo`
  - Facebook Debugger verifierad
  - Favicon: `hjartafavicon.ico`
- **UI-förbättringar**
  - Cookie-banner förbättrad
  - UI-skydd mot dubbelklick vid register/login

### Gemensam logik för Blogg/Arkiv
- Både Blogg och Arkiv-sidorna delar nu samma Razor-markup via `_blogglist` för list- och detaljvy inklusive kommentarsfält.
- Backend-logiken är samlad i **`BloggBasePageModel`** som hanterar laddning av `BloggViewModel`, rollfärger, kommentarer samt flaggan **`openComments`** (som styr om kommentarsfältet är utfällt).
- Detta ersätter tidigare `BloggUiHelpers` och gör att **Blogg/Arkiv är minimala wrappers** som endast anger om sidan kör i arkiv-läge eller ej.
- Resultatet är en **konsekvent och DRY-lösning**: samma logik, samma markup, delat mellan båda vyerna.

---

## Status & kommande arbete
**Klart**
- ✅ Identity-klienten frikopplad – all auth går via API
- ✅ Logout och DeleteAccount städar cookies/token korrekt
- ✅ DataProtection-nycklar delas i DB mellan frontend och API (stabilare cold starts)
- ✅ Datumfält skickas som UTC (`T00:00:00Z`)
- ✅ Förbättrad e-postleverans (SendGrid, svenska ämnesrader)

**Planerat**
- Flytt av frontend till GitHub Pages
- Flerdrag i tarotkortsspelet (förberedelse för betalflöde)

---

## Tarotkortspel (planerat)
- Ny del i SarasBloggAPI
- Kräver inloggning (User eller högre)
- 1–3 kort/dag kopplat till en frågeställning (välj 1, 2 eller alla 3 på en gång)
- AI-granskning av fråga (annan Google-tjänst än Perspective API)
- Förbereda för betalning av extrakort (när företag finns)
- Slumpdragning, visning, AI-genererad tolkning
- Korten designas av Patrik/Sara (högtryckskvalitet för ev. fysisk kortlek)
- Långsiktigt: egen sajt/app för spelet

---

## Snabblänkar
- 🌐 SarasBlogg (frontend): https://sarasblogg.onrender.com
- 🔗 SarasBloggAPI (backend): https://sarasbloggapi.onrender.com
- 🌍 Hilden Media: https://hildenmedia.se
- 💻 GitHub – Hilden202: https://github.com/Hilden202
- 📦 Repo SarasBlogg: https://github.com/Hilden202/SarasBlogg.git
- 🖼 Repo SarasBlogg Media: https://github.com/Hilden202/sarasblogg-media.git
- ⚙️ Repo SarasBlogg API: https://github.com/Hilden202/SarasBloggAPI.git

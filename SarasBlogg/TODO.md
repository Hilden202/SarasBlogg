# TODO – SarasBlogg

## 1. Radering av bild i AboutMe Modalen
- [ ] Implementera en lösning där användaren kan markera att bilden ska tas bort, och raderingen sker först när "Spara ändringar"-knappen klickas.
- [ ] **Plats i projektet:**
  - Razor Page: `Pages/AboutMe.cshtml`
  - PageModel: `Pages/AboutMe.cshtml.cs`

## 2. Klickbar kommentar-räknare i Blogglistan
- När man trycker på antalet kommentarer på ett blogginlägg ska sidan navigera till det inlägget och automatiskt scrolla ner till kommentarssektionen.
- Kommentar-räknaren ska fungera som en klickbar yta utan att se ut som en knapp eller länk.
- **Plats i projektet (tänkta komponenter):**
  - Razor-komponent: `Pages/Shared/_BloggList.cshtml`
  - Eventuell logik: `Pages/Index.cshtml.cs` och/eller `Pages/Arkiv.cshtml.cs`
  - Eventuell back-end/metod i `Services/BloggService.cs`
- Fundera på att implementera en ankar-länk (anchor link) med scroll-funktion i Blazor eller Razor Pages utan extra UI-element.

## 3. Separata felmeddelanden för namn och kommentar vid otillåtet språk
- Visa olika felmeddelanden under respektive fält ("Name" och "Content") i kommentarsformuläret beroende på vilket fält som innehåller otillåtet språk.
- Detta ska göras med `ModelState.AddModelError()` och förbättrad valideringslogik i din PageModel.
- **Kommentar:**  
  Den nuvarande lösningen fungerar redan bra på ett grundläggande sätt:  
  Ett meddelande skrivs alltid ut under kommentarsrutan även om det är namnet som innehåller otillåtet språk.  
  Eftersom felmeddelandet specificerar att det gäller "namnet" upplevs detta ändå som tydligt för användaren.  
  Denna TODO är alltså en framtida förbättring, inte ett akut behov.

## 4. Frikopplad rollhantering via API
- [x] Rollerna hanteras nu helt via SarasBloggAPI, inklusive:
  - Hämta roller
  - Skapa roller
  - Lägga till/ta bort användare från roller
  - Radera roller (med skydd för t.ex. "superadmin")
- [x] Razor Pages använder `UserAPIManager` för alla rollanrop
- [x] Rollkolumner visas i definierad ordning (`user`, `superuser`, `admin`, `superadmin`)
- [ ] Skapa ev. adminfunktion för att redigera rollnamn (ej prioriterat)
- **Plats i projektet:**
  - Razor Page: `Pages/Admin/RoleAdmin/Index.cshtml`
  - PageModel: `Pages/Admin/RoleAdmin/Index.cshtml.cs`
  - API: `SarasBloggAPI.Controllers.RoleController`
  - Tjänst: `UserAPIManager`

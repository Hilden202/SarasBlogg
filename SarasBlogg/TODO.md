# TODO – SarasBlogg

## 1. Radering av profilbild i AboutMe
- [ ] Implementera en lösning där användaren kan markera att bilden ska tas bort, och raderingen sker först när "Spara ändringar"-knappen klickas.
- [ ] **Plats i projektet:**
- Razor Page: `Pages/AboutMe.cshtml`
- PageModel: `Pages/AboutMe.cshtml.cs`

## 2. Klickbar kommentar-räknare i Blogglistan  
   • När man trycker på antalet kommentarer på ett blogginlägg ska sidan navigera till det inlägget och automatiskt scrolla ner till kommentarssektionen. Kommentar-räknaren ska fungera som en klickbar yta utan att se ut som en knapp eller länk.  
   • Plats i projektet (tänkta komponenter):  
     - Razor-komponent: Pages/Shared/_BloggList.cshtml  
     - Eventuell logik: Pages/Index.cshtml.cs och/eller Pages/Arkiv.cshtml.cs  
     - Eventuell back-end/metod i Services/BloggService.cs  
   • Fundera på att implementera en ankar-länk (anchor link) med scroll-funktion i Blazor eller Razor Pages utan extra UI-element.
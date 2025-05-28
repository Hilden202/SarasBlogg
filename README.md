SarasBlogg - Installation och Konfiguration
Denna guide hjälper dig att komma igång med SarasBlogg-projektet, särskilt hantering av känsliga inställningar som admin-användare och databasanslutning.

Viktiga konfigurationsfiler

Projektet använder två viktiga JSON-filer för konfiguration som inte finns med i källkontrollen (.gitignore):

appsettings.json

secrets.json

Appsettings.json innehåller bland annat din databasanslutning. Exempel på innehåll:

{
"ConnectionStrings": {
"DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=SarasBloggDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
}

Secrets.json innehåller uppgifter för den administratörsanvändare som skapas automatiskt vid första start av applikationen. Exempel (byt ut till egna värden):

{
"AdminUser": {
"Email": "din-admin-email@example.com",
"Password": "ditt-säkra-lösenord"
}
}

Viktigt: Ändringar i secrets.json efter att admin-användaren skapats påverkar inte den befintliga användaren. För att ändra admin måste det göras i databasen direkt.

Mappar att skapa

Skapa följande mappar i projektet om de inte redan finns, för att undvika eventuella fel vid körning:

wwwroot/img/blogg/

wwwroot/img/aboutme/

Det är okej om dessa mappar är tomma.

Gitignore

Filen .gitignore är konfigurerad för att inte inkludera känsliga filer som appsettings.json och secrets.json. Detta gör att dessa filer kan innehålla privata uppgifter utan att riskera att läcka på GitHub.

Starta projektet

När du har skapat och lagt till dessa filer och mappar:

Bygg projektet i din utvecklingsmiljö.

Starta applikationen.

Vid första körning skapas administratörsanvändaren automatiskt utifrån värdena i secrets.json.

Logga in med admin-kontot för att administrera sidan.

Tips

Välj ett starkt lösenord för adminanvändaren.

Admin-e-post och lösenord kan ändras via databas eller appens administrationsverktyg (om implementerat).

Behåll känsliga uppgifter utanför GitHub genom att aldrig checka in appsettings.json eller secrets.json.


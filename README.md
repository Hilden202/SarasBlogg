![SarasBlogg](./assets/Sarablogglogga.png)

# SarasBlogg

En bloggplattform byggd i **.NET Razor Pages** med ett separat **API-projekt** för databehandling, bildhantering och AI-funktioner.  
Syftet är att skapa en responsiv, säker och utbyggbar blogg för både publikt läsande och avancerad admin-hantering.

🔗 **Live-sida:** [https://sarasblogg.onrender.com](https://sarasblogg.onrender.com)

---

## 🛠 Teknisk översikt
- **Backend:** .NET Razor Pages (C#) + separat API-projekt  
- **Databas:** Entity Framework Core (PostgreSQL, tidigare SQL Server)  
- **Frontend:** Bootstrap + anpassad CSS  
- **Hosting:** Webbapp och API på Render (frontend planeras flyttas till GitHub Pages)  
- **Kommentarhantering:** AI-analys via Google Perspective API + regex  
- **Kodhantering:** GitHub med aktiv användning av branches  

> **Status:** All auth och rollhantering körs via API:t. Blogg och Arkiv delar logik genom `BloggBasePageModel`. Frontenden har städats från äldre scaffoldad kod.

---

## 📌 Funktioner

### För besökare
- Läsa blogginlägg
- Lämna kommentarer (AI + regex-filtrering)

### Adminfunktioner
- Skapa, redigera, arkivera/dölja blogginlägg  
- Hantera kommentarer och kontaktmeddelanden  
- Rollbaserad åtkomst (User, Superuser, Admin, Superadmin)  
- Inloggning med bekräftad e-post

### Bildhantering
- Bilder sparas i både GitHub och databasen via API  
- Order styr visningsordning, omslagsbild kan bytas  
- Radering av bilder fungerar även för första bilden  

---

## 🚀 API-utveckling
- **Separat projekt:** SarasBloggAPI  
- Driver all logik för auth, kommentarer, bilder, AboutMe och ContactMe  
- Identity och rollhantering helt flyttat till API:t  
- Mål: helt API-drivna klienter (t.ex. appar och fristående frontend)  

---

## 📂 Projektstruktur
SarasBlogg/         # Huvudprojektet med Razor Pages
SarasBloggAPI/      # API-projektet

---

## 📑 Dokumentation
Se **[docs/documentation.md](SarasBlogg/docs/documentation.md)** för teknisk översikt, arkitektur och drift.


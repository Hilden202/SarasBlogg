// För _Layoutsidan <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>
// DarkMode / LightMode från bootstrap med localStorage och har extra finesser <<<>>>>>
document.addEventListener("DOMContentLoaded", function () {
    const savedTheme = localStorage.getItem("theme") || "light";
    document.documentElement.setAttribute("data-bs-theme", savedTheme);
    const toggleButton = document.getElementById("darkModeToggle");

    //Laddar finesser
    updateToggleButtonText(savedTheme);
    updateThemeDependentLinks(savedTheme);
});

function toggleDarkMode() {
    const currentTheme = document.documentElement.getAttribute("data-bs-theme");
    const newTheme = currentTheme === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-bs-theme", newTheme);
    localStorage.setItem("theme", newTheme);

    // Laddar finesser
    updateToggleButtonText(newTheme);
    updateThemeDependentLinks(newTheme);
}

// Extra finesser.
function updateToggleButtonText(theme) {
    const button = document.getElementById("darkModeToggle");
    if (button) {
        button.textContent = theme === "dark" ? "☀️ Ljust läge" : "🌙 Mörkt läge";
    }
}

function updateThemeDependentLinks(theme) {
    const links = document.querySelectorAll(".theme-dependent");
    links.forEach(link => {
        link.style.color = theme === "dark" ? "white" : "black";
    });
}

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>

    // För att gå tillbaka till föregående sida med id
    function reloadCurrentPage(id) {
        window.location.href = window.location.pathname + "?reloadId=" + id;
    }


//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>

// Funktion för att scrolla till ett element om en query-param finns i URL:en
function scrollToSectionIfParamExists(paramName, options = {}) {
    const params = new URLSearchParams(window.location.search);
    if (!paramName || !params.has(paramName)) return;

    const value = params.get(paramName);
    let section = null;

    if (options.prefix) {
        section = document.getElementById(options.prefix + "-" + value);
    } else if (options.id) {
        section = document.getElementById(options.id);
    }

    if (section) {
        section.scrollIntoView({ behavior: 'smooth' });
    }
}

window.addEventListener('DOMContentLoaded', function () {
    scrollToSectionIfParamExists('showId', { id: 'bloggTopSection' });
    scrollToSectionIfParamExists('editId', { id: 'editFormSection' });
    scrollToSectionIfParamExists('reloadId', { prefix: 'reloadPageFormSection' });
    scrollToSectionIfParamExists('commentId', { id: 'commentForm' });
    scrollToSectionIfParamExists('contactId', { id: 'contactForm' });

    // Lägg till fler anrop här om du vill stödja fler parametrar/element
    // scrollToSectionIfParamExists('anotherParam', { id/prefix: 'anotherSectionId'});
});

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

// loading-overlay.js
(function () {
    const overlay = document.getElementById('loading-overlay');
    if (!overlay) return;

    const KEY = 'sb_first_visit_done';
    const SAFETY_MS = 10000;  // hård timeout
    const apiCompleteEvent = 'sb_api_first_load_complete';

    // Om inte första besöket i session → ta bort direkt
    if (sessionStorage.getItem(KEY)) {
        overlay.remove();
        return;
    }

    const startTime = performance.now();

    function hideOverlay(reason) {
        if (!document.body.contains(overlay)) return;
        console.log(`Overlay tas bort pga: ${reason}`);

        overlay.classList.add('hiding'); // CSS: opacity:0, pointer-events:none
        sessionStorage.setItem(KEY, '1');

        setTimeout(() => {
            if (document.body.contains(overlay)) overlay.remove();
        }, 350); // matcha CSS-transition
    }

    // Lyssna på special-event från API-koden när första laddningen är klar
    window.addEventListener(apiCompleteEvent, () => {
        hideOverlay('Första API-anropet klart');
    }, { once: true });

    // Fallback: ta bort overlay när window.load triggas
    window.addEventListener('load', () => {
        // Om API-event inte redan har triggat
        if (document.body.contains(overlay)) {
            hideOverlay('window.load fallback');
        }
    }, { once: true });

    // Hård timeout
    setTimeout(() => {
        if (document.body.contains(overlay)) {
            hideOverlay(`Safety timeout ${SAFETY_MS}ms`);
        }
    }, SAFETY_MS);
})();

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
// --- Miniatyrer styr Bootstrap-karusell ---
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.thumb[data-carousel-id]');
    if (!btn) return;

    const carouselId = btn.dataset.carouselId;
    const index = parseInt(btn.dataset.index, 10);
    const carouselEl = document.getElementById(carouselId);
    if (!carouselEl || Number.isNaN(index)) return;

    const carousel = bootstrap.Carousel.getOrCreateInstance(carouselEl);
    carousel.to(index);
});

// Håll miniatyrerna i synk när man bläddrar i karusellen (pilar, swipe, etc.)
function syncThumbsFor(carouselEl) {
    const id = carouselEl.id;
    const items = carouselEl.querySelectorAll('.carousel-item');
    const activeIndex = Array.from(items).findIndex(x => x.classList.contains('active'));
    const thumbs = document.querySelectorAll(`.thumb-strip .thumb[data-carousel-id="${id}"]`);
    thumbs.forEach((t, i) => {
        t.classList.toggle('active', i === activeIndex);
        t.setAttribute('aria-current', i === activeIndex ? 'true' : 'false');
    });
    const activeThumb = thumbs[activeIndex];
    if (activeThumb) activeThumb.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'smooth' });
}

// Lyssna på alla karuseller på sidan
document.querySelectorAll('.carousel').forEach(el => {
    el.addEventListener('slid.bs.carousel', () => syncThumbsFor(el));
});

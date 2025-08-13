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
(() => {
    const overlay = document.getElementById('loading-overlay');
    if (!overlay) return;

    const KEY = 'sb_first_visit_done_v2';
    const SAFETY_MS = 60000;    // hård timeout (60s)
    const MIN_SHOW_MS = 400;    // undvik blink (0.4s)

    // Visa bara vid första sidvisningen per TAB/SESSION
    if (sessionStorage.getItem(KEY)) {
        overlay.remove();
        return;
    }

    const start = performance.now();

    // Hjälpare: vänta tills alla IMG i dokumentet är klara (eller timeout)
    function waitForImages(timeoutMs) {
        const imgs = Array.from(document.images || []);
        if (imgs.length === 0) return Promise.resolve();

        const promises = imgs.map(img => {
            // redan klar?
            if ((img.complete && img.naturalWidth > 0) || img.dataset.skipDecode === '1') {
                return Promise.resolve();
            }
            // försök decode() för att veta när den är visningsbar
            if (img.decode) {
                return img.decode().catch(() => { }); // svälj decode-fel
            }
            // fallback: lyssna på load/error
            return new Promise(res => {
                const done = () => { img.removeEventListener('load', done); img.removeEventListener('error', done); res(); };
                img.addEventListener('load', done, { once: true });
                img.addEventListener('error', done, { once: true });
            });
        });

        const all = Promise.allSettled(promises);
        const to = new Promise(res => setTimeout(res, timeoutMs));
        return Promise.race([all, to]);
    }

    // Vänta på window.load (allt statiskt klart)
    function waitForWindowLoad() {
        if (document.readyState === 'complete') return Promise.resolve();
        return new Promise(res => window.addEventListener('load', () => res(), { once: true }));
    }

    // Om du i framtiden vill signalera från klientkod att “första data är klar”,
    // kan du dispatcha: window.dispatchEvent(new Event('sb_api_first_load_complete'));
    function waitForOptionalApiSignal(timeoutMs) {
        return new Promise(res => {
            let t = setTimeout(res, timeoutMs);
            window.addEventListener('sb_api_first_load_complete', () => { clearTimeout(t); res(); }, { once: true });
        });
    }

    // Kör: load -> (API-signal valfri) -> bilder
    Promise.resolve()
        .then(waitForWindowLoad)
        .then(() => waitForOptionalApiSignal(15000)) // valfri – times out efter 15s
        .then(() => waitForImages(30000))            // vänta på bilder max 30s
        .then(() => {
            const elapsed = performance.now() - start;
            const remain = Math.max(0, MIN_SHOW_MS - elapsed);

            setTimeout(() => {
                overlay.classList.add('hiding');     // du har redan CSS för .hiding
                sessionStorage.setItem(KEY, '1');    // visa inte overlay igen i denna session
                setTimeout(() => overlay.remove(), 350); // matchar din CSS‑transition
            }, remain);
        });

    // Absolut säkerhet
    setTimeout(() => {
        if (document.body.contains(overlay)) {
            overlay.classList.add('hiding');
            sessionStorage.setItem(KEY, '1');
            setTimeout(() => overlay.remove(), 350);
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

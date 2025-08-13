// För _Layoutsidan <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>
// DarkMode / LightMode från bootstrap med localStorage och har extra finesser <<<>>>>>
//document.addEventListener("DOMContentLoaded", function () {
//    const savedTheme = localStorage.getItem("theme") || "light";
//    document.documentElement.setAttribute("data-bs-theme", savedTheme);
//    updateToggleButtonText(savedTheme);
//    updateThemeDependentLinks(savedTheme);
//});

//function toggleDarkMode() {
//    const currentTheme = document.documentElement.getAttribute("data-bs-theme");
//    const newTheme = currentTheme === "dark" ? "light" : "dark";
//    document.documentElement.setAttribute("data-bs-theme", newTheme);
//    localStorage.setItem("theme", newTheme);
//    updateToggleButtonText(newTheme);
//    updateThemeDependentLinks(newTheme);
//}

//function updateToggleButtonText(theme) {
//    const button = document.getElementById("darkModeToggle");
//    if (button) button.textContent = theme === "dark" ? "☀️ Ljust läge" : "🌙 Mörkt läge";
//}

//function updateThemeDependentLinks(theme) {
//    document.querySelectorAll(".theme-dependent").forEach(link => {
//        link.style.color = theme === "dark" ? "white" : "black";
//    });
//}

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

    if (options.prefix) section = document.getElementById(options.prefix + "-" + value);
    else if (options.id) section = document.getElementById(options.id);

    if (section) section.scrollIntoView({ behavior: 'smooth' });
}

window.addEventListener('DOMContentLoaded', function () {
    scrollToSectionIfParamExists('showId', { id: 'bloggTopSection' });
    scrollToSectionIfParamExists('editId', { id: 'editFormSection' });
    scrollToSectionIfParamExists('reloadId', { prefix: 'reloadPageFormSection' });
    scrollToSectionIfParamExists('commentId', { id: 'commentForm' });
    scrollToSectionIfParamExists('contactId', { id: 'contactForm' });
});

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
// --- Laddningsoverlay och progressiv bildinladdning pausad tillfälligt ---
//   Kan aktiveras igen genom att avkommentera hela detta block.
// Kallas från _Layout när första sidans data är klar
//window.sbApiFirstLoadDone = function () {
//    window.dispatchEvent(new Event('sb_api_first_load_complete'));
//};

//// ---- Progressiv bild-inladdning / fade-in (körs vid varje sidladd) ----
//function setupProgressiveImages() {
//    const overlay = document.getElementById('loading-overlay');

//    // 1) Kritiska bilder: markera direkt
//    document.querySelectorAll('img[data-critical="1"]').forEach(img => {
//        img.classList.add('is-loaded');
//    });

//    // 2) Icke-kritiska bilder utanför overlay: lägg på markörer/lyssnare
//    document.querySelectorAll('img:not([data-critical="1"])').forEach(img => {
//        // hoppa över overlay-bilder och de som explicit markerats att inte fadeas
//        if (overlay && overlay.contains(img)) return;
//        if (img.classList.contains('no-fade')) return;

//        const mark = () => img.classList.add('is-loaded');

//        if (img.complete && img.naturalWidth > 0) {
//            // redan i cache → markera direkt
//            mark();
//        } else if (typeof img.decode === 'function') {
//            // snyggare upplevelse där det stöds
//            img.decode().then(mark).catch(mark);
//        } else {
//            // fallback
//            img.addEventListener('load', mark, { once: true });
//            img.addEventListener('error', mark, { once: true });
//        }
//    });
//}

//// Kör progressiv bild-setup på varje sidladd
//document.addEventListener('DOMContentLoaded', setupProgressiveImages);

// loading-overlay.js (med fas-visning + väntan på bilder första sidbesöket)
//(() => {
//    const overlay = document.getElementById('loading-overlay');
//    if (!overlay) return;

//    const KEY = 'sb_first_visit_done_v2';
//    const SAFETY_MS = 60000;  // absolut timeout
//    const MIN_SHOW_MS = 400;    // min total overlay-tid (mot blink)
//    const MIN_PHASE_MS = 350;    // min per fas (så text hinner synas)

//    // Visa overlay bara första sidvisningen per TAB/SESSION
//    if (sessionStorage.getItem(KEY)) { overlay.remove(); return; }

//    const start = performance.now();
//    const statusText = document.getElementById('loader-status-text');
//    const wait = (ms) => new Promise(r => setTimeout(r, ms));

//    let lastPhaseAt = performance.now();
//    function setPhase(text) {
//        if (statusText) statusText.textContent = text;
//        lastPhaseAt = performance.now();
//    }
//    async function ensureMinPhaseTime() {
//        const elapsed = performance.now() - lastPhaseAt;
//        if (elapsed < MIN_PHASE_MS) await wait(MIN_PHASE_MS - elapsed);
//    }

//    async function waitForWindowLoad() {
//        setPhase('Laddar sidan…');
//        if (document.readyState === 'complete') return;
//        await new Promise(res => window.addEventListener('load', res, { once: true }));
//        await ensureMinPhaseTime();
//    }

//    async function waitForOptionalApiSignal(timeoutMs) {
//        setPhase('Hämtar data…');
//        await new Promise(res => {
//            const t = setTimeout(res, timeoutMs); // auto-continue efter timeout
//            window.addEventListener('sb_api_first_load_complete', () => { clearTimeout(t); res(); }, { once: true });
//        });
//        setPhase('Data klar – startar sidan…');
//        await ensureMinPhaseTime();
//    }

//    async function waitForImages(timeoutMs) {
//        setPhase('Laddar bilder…');

//        // Vänta på icke-kritiska bilder utanför overlay (kritiska visas ändå direkt)
//        const allImgs = Array.from(document.images || []);
//        const imgs = allImgs.filter(img =>
//            !(overlay && overlay.contains(img)) &&
//            img.dataset.critical !== '1'
//        );

//        if (imgs.length === 0) { await ensureMinPhaseTime(); return; }

//        const promises = imgs.map(img => {
//            const markLoaded = () => img.classList.add('is-loaded');
//            if (img.complete && img.naturalWidth > 0) { markLoaded(); return Promise.resolve(); }
//            if (typeof img.decode === 'function') {
//                return img.decode().then(markLoaded).catch(() => { markLoaded(); });
//            }
//            return new Promise(res => {
//                const done = () => { img.removeEventListener('load', done); img.removeEventListener('error', done); markLoaded(); res(); };
//                img.addEventListener('load', done, { once: true });
//                img.addEventListener('error', done, { once: true });
//            });
//        });

//        const all = Promise.allSettled(promises);
//        const to = new Promise(res => setTimeout(res, timeoutMs));
//        await Promise.race([all, to]);
//        await ensureMinPhaseTime();
//    }

//    (async () => {
//        try {
//            await waitForWindowLoad();
//            await waitForOptionalApiSignal(15000);
//            await waitForImages(30000);

//            const totalElapsed = performance.now() - start;
//            const remain = Math.max(0, MIN_SHOW_MS - totalElapsed);
//            await wait(remain);

//            setPhase('Klart – startar sidan…');
//            await ensureMinPhaseTime();

//            overlay.classList.add('hiding');
//            sessionStorage.setItem(KEY, '1');
//            setTimeout(() => overlay.remove(), 350);
//        } catch (e) {
//            console.error('Overlay fel:', e);
//            overlay.classList.add('hiding');
//            sessionStorage.setItem(KEY, '1');
//            setTimeout(() => overlay.remove(), 350);
//        }
//    })();

//    // Safety guard
//    setTimeout(() => {
//        if (document.body.contains(overlay)) {
//            overlay.classList.add('hiding');
//            sessionStorage.setItem(KEY, '1');
//            setTimeout(() => overlay.remove(), 350);
//        }
//    }, SAFETY_MS);
//})();

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

// Håll miniatyrerna i synk när man bläddrar i karusellen
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

document.querySelectorAll('.carousel').forEach(el => {
    el.addEventListener('slid.bs.carousel', () => syncThumbsFor(el));
});

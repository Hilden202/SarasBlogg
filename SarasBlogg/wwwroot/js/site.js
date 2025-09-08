﻿// För att gå tillbaka till föregående sida med id
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
    scrollToSectionIfParamExists('highlightId', { prefix: 'reloadPageFormSection' });
});

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


// LISTVY: endast visning (ingen gilla här)
(function () {
    let apiBase = document.documentElement.dataset.apiBaseUrl || '';
    if (!apiBase) return;
    apiBase = apiBase.replace(/\/+$/, '');

    async function refreshListLikes() {
        const nodes = document.querySelectorAll('.js-like');
        if (!nodes.length) return;

        nodes.forEach(async el => {
            const bloggId = el.dataset.bloggId;
            try {
                const res = await fetch(`${apiBase}/api/likes/${bloggId}`);
                if (!res.ok) return;
                const data = await res.json();
                const count = data.count || 0;
                el.textContent = `${count > 0 ? "❤️" : "♡"} ${count}`;
            } catch (e) {
                console.error('GET /likes error', e);
            }
        });
    }

    // Kör nu och vid back/forward
    refreshListLikes();
})();

// DETALJVY: hjärtknapp (TOGGLE) + flagga för snabb uppdatering i listan
(function () {
    let apiBase = document.documentElement.dataset.apiBaseUrl || '';
    if (!apiBase) { console.warn('Likes: apiBase saknas'); return; }
    apiBase = apiBase.replace(/\/+$/, '');

    const btn = document.querySelector('[data-like-btn]');
    if (!btn) return;

    const bloggId = btn.dataset.bloggId;
    const userId = btn.dataset.userId; // tom om utloggad
    const heart = btn.querySelector('.like-heart');
    const countEl = btn.querySelector('.like-count');

    async function loadCount() {
        try {
            const res = await fetch(`${apiBase}/api/likes/${bloggId}/${encodeURIComponent(userId || "")}`);
            if (!res.ok) return;
            const data = await res.json();
            countEl.textContent = data.count;
            if (data.liked) {
                btn.classList.add('liked');
                heart.textContent = '❤️';
            } else {
                btn.classList.remove('liked');
                heart.textContent = '♡';
            }
        } catch (e) {
            console.error('GET /likes error', e);
        }
    }

    async function toggleLike() {
        if (!userId) return;

        btn.disabled = true;
        try {
            if (btn.classList.contains('liked')) {
                // OGILLA
                const res = await fetch(`${apiBase}/api/likes/${bloggId}/${encodeURIComponent(userId)}`, { method: 'DELETE' });
                if (!res.ok) throw new Error('DELETE failed');
                const data = await res.json();
                countEl.textContent = data.count;
                btn.classList.remove('liked');
                heart.textContent = '♡';
                sessionStorage.removeItem(`liked:${bloggId}`);
            } else {
                // GILLA
                const res = await fetch(`${apiBase}/api/likes`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ bloggId: Number(bloggId), userId })
                });
                if (!res.ok) throw new Error('POST failed');
                const data = await res.json();
                countEl.textContent = data.count;
                btn.classList.add('liked');
                heart.textContent = '❤️';
                sessionStorage.setItem(`liked:${bloggId}`, '1');
            }
        } catch (e) {
            console.error('toggleLike error', e);
        } finally {
            btn.disabled = false;
        }
    }

    loadCount();

    if (userId) {
        btn.addEventListener('click', toggleLike);
        btn.disabled = false;
    } else {
        btn.disabled = true;
    }
})();

//skickar kommentar
document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("comment-form");
    const btn = document.getElementById("comment-submit");
    if (!form || !btn) return;

    form.addEventListener("submit", (e) => {
        e.preventDefault();

        if (!btn.disabled) {
            btn.disabled = true;
            btn.setAttribute("aria-busy", "true");
            btn.textContent = "Skickar kommentar…"; // ← ny text
        }

        // Låt browsern måla UI innan submit
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                form.submit();
            });
        });
    });
});

// --- Fullscreen image lightbox (delegated) ---
(() => {
    const modalEl = document.getElementById('imageLightbox');
    const imgEl = document.getElementById('lightboxImg');
    if (!modalEl || !imgEl || !window.bootstrap) return;

    function ensureInstance() {
        let inst = bootstrap.Modal.getInstance(modalEl);
        if (!inst) inst = new bootstrap.Modal(modalEl, { backdrop: false, keyboard: true });
        return inst;
    }

    // Öppna vid klick på valfri bild med .js-lightbox (även i karusell)
    document.addEventListener('click', (e) => {
        const trigger = e.target.closest('.js-lightbox');
        if (!trigger) return;

        // om du vill kunna ange en separat högupplöst version:
        const full = trigger.getAttribute('data-fullsrc');
        imgEl.src = full || trigger.getAttribute('src') || '';

        ensureInstance().show();
    });

    // Stäng vid klick i själva modalfönstret (hela overlayn)
    modalEl.addEventListener('click', () => {
        const inst = bootstrap.Modal.getInstance(modalEl);
        if (inst) inst.hide();
    });

    // Lägg på Escape-lyssnare när modalen öppnas
    modalEl.addEventListener('shown.bs.modal', () => {
        const onKey = (ev) => {
            if (ev.key === 'Escape') {
                const inst = bootstrap.Modal.getInstance(modalEl);
                if (inst) inst.hide();
            }
        };
        modalEl._onKey = onKey;
        document.addEventListener('keydown', onKey);
    });

    // Ta bort Escape-lyssnaren när modalen stängs
    modalEl.addEventListener('hidden.bs.modal', () => {
        if (modalEl._onKey) {
            document.removeEventListener('keydown', modalEl._onKey);
            modalEl._onKey = null;
        }
    });
})();

// --- Cookie accept banner script ---
document.addEventListener('DOMContentLoaded', function () {
    const banner = document.getElementById("cookieConsentdiv");
    const body = document.body;
    if (!banner) return;

    const has = (name, val) =>
        document.cookie.split("; ").some(c =>
            c.indexOf(name + "=") === 0 && (!val || c.endsWith("=" + val))
        );

    const accepted =
        has(".AspNetCore.Consent", "yes") ||
        has(".AspNet.Consent", "yes");

    if (accepted) {
        banner.style.display = "none";
        return;
    }

    const toggle = document.getElementById("showPrivacyContent");
    if (toggle) {
        toggle.addEventListener("click", function (e) {
            e.preventDefault();
            const box = document.getElementById("privacyContent");
            const show = box.style.display !== "block";
            box.style.display = show ? "block" : "none";
            toggle.textContent = show ? "Visa mindre" : "Läs mer här";
            body.classList.toggle("modal-open", show);
        });
    }

    const btn = banner.querySelector("button[data-cookie-string]");
    if (!btn) return;

    btn.addEventListener("click", function () {
        const serverCookie = btn.getAttribute("data-cookie-string");
        if (serverCookie) document.cookie = serverCookie;

        const secure = location.protocol === "https:" ? "; Secure" : "";
        const commonAttrs = "; Path=/; Max-Age=31536000; SameSite=Lax" + secure;
        document.cookie = ".AspNetCore.Consent=yes" + commonAttrs;
        document.cookie = ".AspNet.Consent=yes" + commonAttrs;

        banner.style.transition = "opacity .25s ease";
        banner.style.opacity = "0";
        setTimeout(() => {
            banner.style.display = "none";
            body.classList.remove("modal-open");
        }, 250);
    });
});
// --- End cookie accept banner script ---

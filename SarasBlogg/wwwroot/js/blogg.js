// Batch-laddning av äldre kommentarer (per inlägg) – visa alltid de sista dolda
(function () {
    function setup(btn) {
        var containerSel = btn.getAttribute('data-container');
        var batch = parseInt(btn.getAttribute('data-batch') || '10', 10);

        function remaining(container) {
            return container.querySelectorAll('.js-older.d-none').length;
        }

        function updateLabel() {
            var container = document.querySelector(containerSel);
            if (!container) return;
            var hidden = remaining(container);
            if (hidden <= 0) {
                btn.classList.add('d-none');
            } else {
                btn.classList.remove('d-none');
                btn.textContent = 'Visa ' + Math.min(batch, hidden) + ' äldre (' + hidden + ' återstår)';
            }
        }

        btn.addEventListener('click', function () {
            var container = document.querySelector(containerSel);
            if (!container) return;

            // Ta de SISTA dolda elementen så att nya batchen lägger sig närmast "recent"
            var hidden = Array.from(container.querySelectorAll('.js-older.d-none'));
            var toShow = hidden.slice(-batch); // <-- nyckeln
            toShow.forEach(function (el) { el.classList.remove('d-none'); });

            updateLabel();
        });

        updateLabel();
    }

    document.querySelectorAll('.js-load-older').forEach(setup);
})();
// Delete Comment helper
// Hybrid-confirm: använd snygg modal om showConfirm() finns, annars native confirm()
function handleDeleteSubmit(e, form) {
    const opts = form.getAttribute('data-confirm') || 'Ta bort?';
    if (typeof window.showConfirm === 'function') {
        e.preventDefault();
        window.showConfirm(opts).then(ok => { if (ok) form.submit(); });
        return false;
    }
    // Fallback: native confirm måste få en sträng
    return window.confirm(typeof opts === 'string' ? opts : 'Ta bort?');
}

// Knapp-varianten
function handleDeleteButton(e, messageOrOptions) {
    const btn = e.currentTarget;
    const form = btn.form;

    // Bygg vettig text även om vi bara har fallback (native confirm)
    const fallbackMsg = (typeof messageOrOptions === 'string')
        ? messageOrOptions
        : (messageOrOptions && messageOrOptions.text) || 'Är du säker?';

    if (typeof window.showConfirm === 'function') {
        e.preventDefault();
        window.showConfirm(messageOrOptions).then(ok => {
            if (ok && form) {
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit(btn);      // bevarar validering/handler
                } else {
                    form.submit();
                }
            }
        });
        return false; // stoppa default
    }

    // Fallback: native confirm (måste få sträng, inte objekt!)
    return window.confirm(fallbackMsg);
}

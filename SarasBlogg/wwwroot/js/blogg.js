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
    const msg = form.getAttribute('data-confirm') || 'Ta bort kommentaren?';

    // Om vår custom modal är inladdad
    if (typeof window.showConfirm === 'function') {
        e.preventDefault(); // stoppa POST tills vi vet svaret
        window.showConfirm(msg).then(ok => {
            if (ok) form.submit(); // kör vanlig POST
        });
        return false; // för säkerhets skull – förhindra submit här
    }

    // Fallback: inbyggd confirm (iOS visar domän, men funkar)
    return window.confirm(msg);
}

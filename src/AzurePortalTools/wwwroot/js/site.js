// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Loading spinner on form submit and select change
document.addEventListener('DOMContentLoaded', function () {
    var overlay = document.getElementById('loading-overlay');
    var loadingText = document.getElementById('loading-text');
    if (!overlay) return;

    function showSpinner(message) {
        if (loadingText) loadingText.textContent = message || 'Operazione in corso...';
        overlay.style.display = 'flex';
    }

    // Intercept all form submissions (POST)
    document.querySelectorAll('form[method="post"]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            // If confirm() was cancelled, don't show spinner
            // The confirm is handled by onclick, so if we reach here it was accepted
            var btn = form.querySelector('button[type="submit"]:focus, button[type="submit"]:active');
            var message = 'Operazione in corso...';

            if (btn) {
                var text = btn.textContent.trim();
                if (text.includes('Avvia')) message = 'Avvio VM in corso...';
                else if (text.includes('Ferma')) message = 'Arresto VM in corso...';
                else if (text.includes('Applica')) message = 'Applicazione regole NSG...';
            }

            showSpinner(message);
        });
    });

    // Intercept select changes that trigger form submit (GET navigations)
    document.querySelectorAll('select[onchange="this.form.submit()"]').forEach(function (sel) {
        sel.addEventListener('change', function () {
            showSpinner('Caricamento...');
        });
    });

    // Also show spinner on regular link navigations to tenant pages
    document.querySelectorAll('a[href*="tenantId"]').forEach(function (link) {
        link.addEventListener('click', function () {
            showSpinner('Caricamento...');
        });
    });
});

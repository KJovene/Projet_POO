// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ----- Dark mode toggle -----
(function () {
    var btn = document.getElementById('rvThemeToggle');
    if (!btn) return;

    function sync() {
        var dark = document.documentElement.getAttribute('data-theme') === 'dark';
        btn.textContent = dark ? '☀️' : '🌙';
        btn.setAttribute('aria-pressed', dark ? 'true' : 'false');
    }

    sync();

    btn.addEventListener('click', function () {
        var dark = document.documentElement.getAttribute('data-theme') === 'dark';
        var next = dark ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        try { localStorage.setItem('locatic-theme', next); } catch (e) { }
        sync();
    });
})();

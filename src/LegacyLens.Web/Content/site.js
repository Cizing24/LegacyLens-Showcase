document.querySelectorAll('.sidebar a').forEach(function (link) {
  if (link.pathname === location.pathname) link.setAttribute('aria-current', 'page');
});
document.querySelectorAll('form').forEach(function (form) {
  form.addEventListener('submit', function () {
    form.classList.add('busy');
    form.setAttribute('aria-busy', 'true');
  });
});
// Native submission avoids Web Forms' string-based setTimeout, which CSP blocks.
document.querySelectorAll('select[data-postback]').forEach(function (select) {
  select.addEventListener('change', function () { select.form.requestSubmit(); });
});

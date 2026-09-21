const colorScheme = window.matchMedia('(prefers-color-scheme: dark)');

function updateTheme() {
  const theme = colorScheme.matches ? 'dark' : 'light';
  if (document.documentElement.getAttribute('data-bs-theme') !== theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);
  }
}

new MutationObserver(updateTheme).observe(document.documentElement, {
  attributes: true,
  attributeFilter: ['data-bs-theme'],
});

updateTheme();
colorScheme.addEventListener('change', updateTheme);

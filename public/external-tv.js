const buttons = document.querySelectorAll('button[data-site]');
const frame = document.getElementById('site-frame');
const openSite = document.getElementById('open-site');

for (const button of buttons) {
  button.addEventListener('click', () => {
    for (const item of buttons) {
      item.setAttribute('aria-pressed', String(item === button));
    }
    frame.src = button.dataset.site;
    frame.title = button.textContent;
    frame.hidden = false;
    openSite.href = button.dataset.site;
    openSite.hidden = false;
  });
}

const selectedSite = new URLSearchParams(location.search).get('site');
for (const button of buttons) {
  if (button.dataset.key === selectedSite) button.click();
}

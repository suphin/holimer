(function () {
    'use strict';
    const root = document.getElementById('borcAlacakReport');
    const dialog = document.getElementById('baMovementDialog');
    if (!root || !dialog) return;
    const content = document.getElementById('baMovementContent');
    const status = document.getElementById('baMovementStatus');
    let currentUrl;
    let activeTrigger;
    let pending;
    let sequence = 0;

    async function load(page) {
        if (pending) pending.abort();
        pending = new AbortController();
        const request = ++sequence;
        const url = new URL(currentUrl, window.location.href);
        url.searchParams.set('page', String(page));
        content.innerHTML = '';
        content.setAttribute('aria-busy', 'true');
        status.hidden = false;
        status.classList.remove('is-error');
        status.textContent = 'Cari hareketler yükleniyor…';
        try {
            const response = await fetch(url, { credentials: 'same-origin', cache: 'no-store', signal: pending.signal,
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'text/html' } });
            if (response.redirected || response.status === 401 || response.status === 403)
                throw new Error('Oturumunuz sona ermiş veya bu raporu görüntüleme yetkiniz yok. Sayfayı yenileyip tekrar deneyiniz.');
            if (!response.ok || response.headers.get('X-Report-Partial') !== 'CariHareketler')
                throw new Error('Cari hareketler alınamadı. Lütfen pencereyi kapatıp tekrar deneyiniz.');
            const html = await response.text();
            if (request !== sequence || !dialog.open) return;
            // Only the authenticated, server-rendered Razor partial with its expected marker is accepted.
            // All data fields in that partial are HTML-encoded by Razor.
            content.innerHTML = html;
            status.hidden = true;
        } catch (error) {
            if (error.name === 'AbortError' || request !== sequence || !dialog.open) return;
            status.classList.add('is-error');
            status.textContent = error instanceof TypeError ? 'Bağlantı kurulamadı. Lütfen tekrar deneyiniz.' : error.message;
        } finally {
            if (request === sequence) content.setAttribute('aria-busy', 'false');
        }
    }
    root.addEventListener('click', event => {
        const trigger = event.target.closest('.ba-detail-trigger');
        if (!trigger || !root.contains(trigger)) return;
        if (typeof dialog.showModal !== 'function') {
            window.alert('Bu pencere için güncel bir tarayıcı kullanınız.');
            return;
        }
        activeTrigger = trigger;
        currentUrl = trigger.dataset.url;
        dialog.showModal();
        load(1);
    });
    content.addEventListener('click', event => {
        const button = event.target.closest('[data-movement-page]');
        if (!button || button.disabled) return;
        const page = Number(button.dataset.movementPage);
        if (Number.isSafeInteger(page) && page > 0) load(page);
    });
    dialog.querySelector('.ba-dialog-close').addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', () => {
        sequence++;
        if (pending) pending.abort();
        content.innerHTML = '';
        content.setAttribute('aria-busy', 'false');
        status.hidden = true;
        if (activeTrigger && activeTrigger.isConnected) activeTrigger.focus();
    });
})();

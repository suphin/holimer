(function () {
    'use strict';

    const root = document.getElementById('borcAlacakReport');
    if (!root) return;
    const find = selector => root.querySelector(selector);
    const search = find('#baSearch');
    const group = find('#baGroup');
    const pageSize = find('#baPageSize');
    const body = find('#tblCari tbody');
    const empty = find('#baEmpty');
    const cards = Array.from(root.querySelectorAll('.ba-summary'));
    const sortButtons = Array.from(root.querySelectorAll('[data-sort]'));
    const collator = new Intl.Collator('tr-TR', { numeric: true, sensitivity: 'base' });
    const number = new Intl.NumberFormat('tr-TR');
    // Keep decimal arithmetic exact (including fractional cents); round only the displayed total.
    const scale = 10n ** 28n;
    const centScale = scale / 100n;
    function decimal(raw) {
        const negative = raw.startsWith('-');
        const parts = (negative ? raw.slice(1) : raw).split('.');
        const value = BigInt(parts[0]) * scale + BigInt((parts[1] || '').padEnd(28, '0'));
        return negative ? -value : value;
    }
    function amount(value) {
        const negative = value < 0n;
        const absolute = negative ? -value : value;
        let cents = absolute / centScale;
        const remainder = absolute % centScale;
        if (remainder * 2n > centScale || (remainder * 2n === centScale && cents % 2n !== 0n)) cents++;
        return (negative && cents !== 0n ? '-' : '') + number.format(cents / 100n) + ',' + String(cents % 100n).padStart(2, '0');
    }
    function normalize(text) {
        return text.toLocaleLowerCase('tr-TR').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/ı/g, 'i');
    }
    const rows = Array.from(root.querySelectorAll('[data-account-row]')).map(element => {
        const debit = decimal(element.dataset.debit);
        const credit = decimal(element.dataset.credit);
        return { element, code: element.dataset.code, name: element.dataset.name, group: element.dataset.group,
            search: normalize(element.dataset.code + ' ' + element.dataset.name), debit, credit, net: credit - debit };
    });
    let page = 1;
    let sortKey = 'code';
    let direction = 1;

    function render() {
        const terms = normalize(search.value.trim()).split(/\s+/).filter(Boolean);
        const matched = rows.filter(row => (!group.value || row.group === group.value) && terms.every(term => row.search.includes(term)));
        matched.sort((a, b) => {
            const left = a[sortKey], right = b[sortKey];
            const order = typeof left === 'bigint' ? (left < right ? -1 : left > right ? 1 : 0) : collator.compare(left, right);
            return direction * order || collator.compare(a.code, b.code);
        });
        const size = pageSize.value === 'all' ? Math.max(matched.length, 1) : Number(pageSize.value);
        const pages = Math.max(1, Math.ceil(matched.length / size));
        page = Math.max(1, Math.min(page, pages));
        rows.forEach(row => { row.element.hidden = true; });
        const start = (page - 1) * size;
        const fragment = document.createDocumentFragment();
        matched.slice(start, start + size).forEach(row => { row.element.hidden = false; fragment.appendChild(row.element); });
        body.insertBefore(fragment, empty);
        empty.hidden = matched.length !== 0;
        const totals = matched.reduce((sum, row) => ({ debit: sum.debit + row.debit, credit: sum.credit + row.credit }), { debit: 0n, credit: 0n });
        find('#baDebitTotal').textContent = amount(totals.debit);
        find('#baCreditTotal').textContent = amount(totals.credit);
        find('#baNetTotal').textContent = amount(totals.credit - totals.debit);
        find('#baResultCount').textContent = number.format(matched.length) + ' / ' + number.format(rows.length) + ' cari hesap';
        find('#baPageInfo').textContent = matched.length === 0 ? 'Gösterilecek kayıt yok' :
            number.format(start + 1) + '–' + number.format(Math.min(start + size, matched.length)) + ' / ' + number.format(matched.length) + ' kayıt';
        find('#baPageNumber').textContent = page + ' / ' + pages;
        find('#baPrevious').disabled = page <= 1;
        find('#baNext').disabled = page >= pages;
        cards.forEach(card => {
            const active = card.dataset.group === group.value;
            card.classList.toggle('is-active', active);
            card.setAttribute('aria-pressed', String(active));
        });
        sortButtons.forEach(button => {
            const active = button.dataset.sort === sortKey;
            button.closest('th').setAttribute('aria-sort', active ? (direction === 1 ? 'ascending' : 'descending') : 'none');
            button.querySelector('span').textContent = active ? (direction === 1 ? '↑' : '↓') : '↕';
        });
    }
    function filter() { page = 1; render(); }
    search.addEventListener('input', filter);
    group.addEventListener('change', filter);
    pageSize.addEventListener('change', filter);
    cards.forEach(card => card.addEventListener('click', () => { group.value = card.dataset.group; filter(); }));
    find('#baReset').addEventListener('click', () => { search.value = ''; group.value = ''; filter(); search.focus(); });
    find('#baPrevious').addEventListener('click', () => { page--; render(); });
    find('#baNext').addEventListener('click', () => { page++; render(); });
    sortButtons.forEach(button => button.addEventListener('click', () => {
        if (sortKey === button.dataset.sort) direction *= -1;
        else { sortKey = button.dataset.sort; direction = 1; }
        page = 1;
        render();
    }));
    render();
})();

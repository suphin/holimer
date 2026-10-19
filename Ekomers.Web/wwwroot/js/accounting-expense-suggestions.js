(function () {
    'use strict';
    var form = document.getElementById('expenseSuggestionApproval');
    if (!form) return;
    var search = document.getElementById('suggestionAccountSearch');
    var all = document.getElementById('suggestionSelectVisible');
    var count = document.getElementById('suggestionSelectionCount');
    var empty = document.getElementById('suggestionSearchEmpty');
    var approve = document.getElementById('suggestionApprove');
    var rows = Array.from(form.querySelectorAll('[data-suggestion-account]'));
    function normalized(value) { return value.toLocaleLowerCase('tr-TR').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/ı/g, 'i'); }
    function box(row) { return row.querySelector('[data-suggestion-select]'); }
    function group(row) { return row.querySelector('[data-suggestion-group]'); }
    function sync() {
        var selected = rows.filter(function (row) { return box(row).checked; });
        count.textContent = selected.length + ' hesap seçildi';
        approve.disabled = selected.length === 0;
        rows.forEach(function (row) { group(row).required = box(row).checked; });
        var eligible = rows.filter(function (row) { return row.style.display !== 'none' && group(row).value; });
        var checked = eligible.filter(function (row) { return box(row).checked; }).length;
        all.checked = eligible.length > 0 && checked === eligible.length;
        all.indeterminate = checked > 0 && checked < eligible.length;
    }
    search.addEventListener('input', function () {
        var terms = normalized(search.value).trim().split(/\s+/).filter(Boolean);
        var visible = 0;
        rows.forEach(function (row) {
            var text = normalized(row.dataset.suggestionAccount);
            var match = terms.every(function (term) { return text.includes(term); });
            row.style.display = match ? '' : 'none';
            if (match) visible++; else box(row).checked = false;
        });
        empty.style.display = visible ? 'none' : '';
        sync();
    });
    all.addEventListener('change', function () {
        rows.forEach(function (row) { if (row.style.display !== 'none' && group(row).value) box(row).checked = all.checked; });
        sync();
    });
    rows.forEach(function (row) {
        box(row).addEventListener('change', sync);
        group(row).addEventListener('change', sync);
    });
    form.addEventListener('submit', function (event) {
        var selected = rows.filter(function (row) { return box(row).checked; });
        if (!selected.length || selected.some(function (row) { return !group(row).value; }) ||
            !window.confirm(selected.length + ' hesap seçilen gruplara bağlansın mı? Geçerlilik tarihleri geçmiş raporları etkileyebilir. Mevcut eşleştirmeler değiştirilmez.')) event.preventDefault();
    });
    sync();
})();

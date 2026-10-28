(function () {
    'use strict';
    function start() {
        var jq = window.jQuery;
        if (!jq || !jq.fn || typeof jq.fn.select2 !== 'function') return;
        function fold(text) {
            return String(text || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/ı/g, 'i');
        }
        document.querySelectorAll('select.transfer-lot, select.transfer-warehouse').forEach(function (select) {
            if (jq(select).data('select2')) return;
            jq(select).select2({
                width: '100%', placeholder: select.dataset.placeholder, allowClear: true,
                minimumResultsForSearch: 0,
                matcher: function (params, data) {
                    var words = fold(params.term).trim().split(/\s+/).filter(Boolean);
                    return words.every(function (word) { return fold(data.text).indexOf(word) !== -1; }) ? data : null;
                },
                language: {
                    noResults: function () { return select.classList.contains('transfer-warehouse') ? 'Eşleşen depo bulunamadı.' : 'Eşleşen ürün / lot bulunamadı.'; },
                    searching: function () { return 'Aranıyor…'; }
                }
            });
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true });
    else start();
})();

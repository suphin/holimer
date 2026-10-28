(function () {
    'use strict';
    function start() {
        var form = document.getElementById('fasonReceiptForm');
        if (!form || form.dataset.ready) return;
        form.dataset.ready = 'true';
        var body = document.getElementById('fasonLines'), jq = window.jQuery;
        var select2 = jq && jq.fn && typeof jq.fn.select2 === 'function';
        function fold(text) { return String(text || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/ı/g, 'i'); }
        function init(select) {
            if (!select2 || jq(select).data('select2')) return;
            jq(select).select2({ width: '100%', placeholder: select.dataset.placeholder, allowClear: true, minimumResultsForSearch: 0,
                matcher: function (params, data) { var words = fold(params.term).trim().split(/\s+/).filter(Boolean); return words.every(function (word) { return fold(data.text).includes(word); }) ? data : null; },
                language: { noResults: function () { return 'Eşleşen kayıt bulunamadı.'; } } });
        }
        function destroy(select) { if (select2 && jq(select).data('select2')) jq(select).select2('destroy'); }
        function reindex() {
            body.querySelectorAll('.fason-row').forEach(function (row, i) {
                row.querySelectorAll('select[name], input[name]').forEach(function (control) {
                    var name = control.name.replace(/Lines\[[^\]]+\]/, 'Lines[' + i + ']');
                    var id = name.replace(/\[/g, '_').replace(/\]\./g, '__').replace(/\./g, '_');
                    if (control.tagName === 'SELECT' && control.id !== id) destroy(control);
                    control.name = name; control.id = id;
                });
                row.querySelectorAll('select.fason-search').forEach(init);
            });
        }
        form.querySelectorAll('select.fason-search').forEach(init);
        document.getElementById('addFasonLine').addEventListener('click', function () {
            if (body.querySelectorAll('.fason-row').length >= 100) { window.alert('En fazla 100 satır girilebilir.'); return; }
            body.appendChild(document.getElementById('fasonLineTemplate').content.cloneNode(true)); reindex();
        });
        body.addEventListener('click', function (event) {
            var button = event.target.closest('.fason-remove'); if (!button) return;
            var row = button.closest('.fason-row'); row.querySelectorAll('select').forEach(destroy); row.remove(); reindex();
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true }); else start();
})();

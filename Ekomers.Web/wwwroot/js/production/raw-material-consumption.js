(function () {
    'use strict';
    function start() {
        var form = document.getElementById('rawMaterialConsumptionForm');
        var body = document.getElementById('sarfLines');
        if (!form || !body || form.dataset.pickerReady) return;
        form.dataset.pickerReady = 'true';
        var jq = window.jQuery;
        var hasSelect2 = jq && jq.fn && typeof jq.fn.select2 === 'function';

        function fold(text) {
            return String(text || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/ı/g, 'i');
        }
        function matcher(params, data) {
            var words = fold(params.term).trim().split(/\s+/).filter(Boolean);
            return words.every(function (word) { return fold(data.text).indexOf(word) !== -1; }) ? data : null;
        }
        function refresh(select) {
            var row = select.closest('.sarf-row');
            var option = select.options[select.selectedIndex];
            row.querySelector('.sarf-info').textContent = !option || !option.value ? 'Lot seçiniz' :
                option.dataset.unavailable ? 'Bu lot artık kullanılamıyor; başka lot seçiniz.' :
                option.dataset.available + ' ' + option.dataset.unit;
            row.querySelector('.sarf-quantity').setAttribute('aria-label', 'Sarf miktarı' +
                (option && option.value && option.dataset.unit ? ' (' + option.dataset.unit + ')' : ''));
        }
        function destroy(select) {
            if (!jq) return;
            jq(select).off('.rawMaterialConsumption');
            if (hasSelect2 && jq(select).data('select2')) jq(select).select2('destroy');
        }
        function initialize(select) {
            if (hasSelect2 && !jq(select).data('select2')) {
                jq(select).select2({
                    width: '100%', placeholder: select.dataset.placeholder, allowClear: true,
                    minimumResultsForSearch: 0, matcher: matcher,
                    language: { noResults: function () { return 'Eşleşen hammadde / lot bulunamadı.'; }, searching: function () { return 'Aranıyor…'; } }
                });
            }
            // Select2 triggers jQuery change events, which need not invoke a native delegated listener.
            if (jq) jq(select).off('change.rawMaterialConsumption').on('change.rawMaterialConsumption', function () { refresh(select); });
            refresh(select);
        }
        function reindex() {
            body.querySelectorAll('.sarf-row').forEach(function (row, index) {
                var select = row.querySelector('.sarf-lot');
                var nextId = 'Lines_' + index + '__StockLotId';
                if (select.id !== nextId) destroy(select);
                // Only original controls: never rename Select2's generated search field.
                row.querySelectorAll('select[name], input[name]').forEach(function (control) {
                    control.name = control.name.replace(/Lines\[[^\]]+\]/, 'Lines[' + index + ']');
                    control.id = control.name.replace(/\[/g, '_').replace(/\]\./g, '__').replace(/\./g, '_');
                });
                initialize(select);
            });
        }
        body.addEventListener('change', function (event) {
            if (event.target.matches('.sarf-lot')) refresh(event.target);
        });
        body.addEventListener('click', function (event) {
            var button = event.target.closest('.sarf-remove');
            if (!button) return;
            var row = button.closest('.sarf-row');
            destroy(row.querySelector('.sarf-lot'));
            row.remove(); reindex();
        });
        document.getElementById('addSarfLine').addEventListener('click', function () {
            if (body.querySelectorAll('.sarf-row').length >= 100) { window.alert('En fazla 100 satır eklenebilir.'); return; }
            body.appendChild(document.getElementById('sarfLineTemplate').content.cloneNode(true));
            reindex();
        });
        reindex();
        var warehouse = document.getElementById('warehouseFilter');
        warehouse.value = form.querySelector('input[name="WarehouseId"]').value;
        if (hasSelect2) jq(warehouse).select2({ width: '100%', placeholder: 'Depo seçiniz', allowClear: true, matcher: matcher });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true });
    else start();
})();

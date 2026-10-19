(function () {
    'use strict';

    var search = document.getElementById('recipeCostProductSearch');
    var body = document.getElementById('recipeCostProductRows');
    var clear = document.getElementById('recipeCostProductSearchClear');
    var count = document.getElementById('recipeCostProductSearchCount');
    var empty = document.getElementById('recipeCostProductSearchEmpty');
    if (!search || !body || !clear || !count || !empty) return;

    function normalize(value) {
        return value.toLocaleLowerCase('tr-TR').normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '').replace(/ı/g, 'i');
    }

    var products = Array.from(body.querySelectorAll('[data-product-search]')).map(function (row) {
        return { row: row, text: normalize(row.dataset.productSearch) };
    });

    function filter() {
        var terms = normalize(search.value).trim().split(/\s+/).filter(Boolean);
        var visible = 0;
        products.forEach(function (product) {
            var matches = terms.every(function (term) { return product.text.includes(term); });
            product.row.style.display = matches ? '' : 'none';
            if (matches) visible++;
        });
        count.textContent = visible + ' / ' + products.length + ' ürün gösteriliyor';
        empty.style.display = visible === 0 ? '' : 'none';
    }

    search.addEventListener('input', filter);
    clear.addEventListener('click', function () {
        search.value = '';
        filter();
        search.focus();
    });
    search.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            search.value = '';
            filter();
        }
    });
    filter();
})();

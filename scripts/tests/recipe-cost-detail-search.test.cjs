const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../..');
const source = fs.readFileSync(path.join(root, 'Ekomers.Web/wwwroot/js/recipe-cost-detail-search.js'), 'utf8');

function setup(texts) {
    const rows = texts.map(text => ({ dataset: { productSearch: text }, style: {} }));
    const elements = {};
    for (const name of ['Search', 'SearchClear', 'SearchCount', 'SearchEmpty']) {
        elements['recipeCostProduct' + name] = {
            value: '', style: {}, textContent: '', events: {},
            addEventListener(type, fn) { this.events[type] = fn; },
            focus() { this.focused = true; }
        };
    }
    elements.recipeCostProductRows = { querySelectorAll() { return rows; } };
    vm.runInNewContext(source, { document: { getElementById(id) { return elements[id]; } } });
    const search = elements.recipeCostProductSearch;
    return { rows, elements, search, filter(value) { search.value = value; search.events.input(); } };
}

const page = setup([
    '152MM.01.02.001 MORE THAN ETERNAMİX YEŞİL ÇAY EKSTRESİ 60 KAPSÜL',
    '152MM.01.02.004 FISH OIL ŞİŞE',
    '152MM.02.01.003 Vitamin C'
]);
const visible = () => page.rows.map(row => row.style.display !== 'none');
assert.deepEqual(visible(), [true, true, true]);
page.filter('152mm.01.02.004');
assert.deepEqual(visible(), [false, true, false]);
page.filter('YESIL cay');
assert.deepEqual(visible(), [true, false, false]);
page.filter('  152MM.01   kapsul  ');
assert.deepEqual(visible(), [true, false, false]);
page.filter('fish sise');
assert.deepEqual(visible(), [false, true, false]);
page.filter('olmayan ürün');
assert.deepEqual(visible(), [false, false, false]);
assert.equal(page.elements.recipeCostProductSearchEmpty.style.display, '');
assert.equal(page.elements.recipeCostProductSearchCount.textContent, '0 / 3 ürün gösteriliyor');
page.elements.recipeCostProductSearchClear.events.click();
assert.equal(page.search.value, '');
assert.equal(page.search.focused, true);
assert.deepEqual(visible(), [true, true, true]);
assert.equal(page.elements.recipeCostProductSearchEmpty.style.display, 'none');
page.filter('vitamin');
page.search.events.keydown({ key: 'Escape' });
assert.deepEqual(visible(), [true, true, true]);
page.filter('    ');
assert.deepEqual(visible(), [true, true, true]);
page.filter('<script>');
assert.deepEqual(visible(), [false, false, false]);
const empty = setup([]);
assert.equal(empty.elements.recipeCostProductSearchCount.textContent, '0 / 0 ürün gösteriliyor');
assert.equal(empty.elements.recipeCostProductSearchEmpty.style.display, '');
vm.runInNewContext(source, { document: { getElementById() { return null; } } });

const view = fs.readFileSync(path.join(root, 'Ekomers.Web/Views/ReceteMaliyetleri/KayitDetay.cshtml'), 'utf8');
assert.match(view, /data-product-search="@\(line.ProductCode \+ " " \+ line.ProductName\)"/);
assert.match(view, /@section JSSection/);
assert.match(view, /recipe-cost-detail-search\.js" asp-append-version="true"/);
assert.match(view, /aria-controls="recipeCostProductRows"/);
assert.match(view, /colspan="8"/);
console.log('Recipe cost detail product search: all tests passed.');

// Run: node scripts/tests/borc-alacak.test.cjs (no packages or database needed).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../../Ekomers.Web/wwwroot/js/reports/borc-alacak.js'), 'utf8');

class Element {
    constructor(dataset = {}) {
        this.dataset = dataset; this.hidden = false; this.value = ''; this.textContent = '';
        this.children = []; this.attributes = {}; this.events = {};
        this.classList = { toggle: (key, active) => { this.attributes[key] = active; } };
    }
    addEventListener(type, listener) { (this.events[type] ||= []).push(listener); }
    fire(type) { for (const listener of this.events[type] || []) listener({ target: this }); }
    setAttribute(key, value) { this.attributes[key] = value; }
    closest() { return this.heading; }
    querySelector() { return this.icon; }
    focus() { this.focused = true; }
    appendChild(child) {
        if (child.parent) child.parent.children.splice(child.parent.children.indexOf(child), 1);
        child.parent = this; this.children.push(child);
    }
    insertBefore(fragment, before) {
        for (const child of [...fragment.children]) {
            if (child.parent) child.parent.children.splice(child.parent.children.indexOf(child), 1);
            child.parent = this; this.children.splice(this.children.indexOf(before), 0, child);
        }
    }
}
function setup(data) {
    const elements = Object.fromEntries(['baSearch', 'baGroup', 'baPageSize', 'baEmpty', 'baDebitTotal', 'baCreditTotal',
        'baNetTotal', 'baResultCount', 'baPageInfo', 'baPageNumber', 'baPrevious', 'baNext', 'baReset'].map(id => ['#' + id, new Element()]));
    elements['#baPageSize'].value = '25';
    const body = new Element(); elements['#tblCari tbody'] = body;
    const rows = data.map(dataset => new Element(dataset));
    rows.forEach(row => body.appendChild(row)); body.appendChild(elements['#baEmpty']);
    const cards = ['', '120', '320', '329', '399', 'Diger'].map(group => new Element({ group }));
    const sorts = ['code', 'name', 'debit', 'credit', 'net'].map(sort => {
        const button = new Element({ sort }); button.heading = new Element(); button.icon = new Element(); return button;
    });
    const root = {
        querySelector: selector => elements[selector],
        querySelectorAll: selector => ({ '.ba-summary': cards, '[data-sort]': sorts, '[data-account-row]': rows })[selector]
    };
    vm.runInNewContext(source, { document: { getElementById: () => root, createDocumentFragment: () => new Element() }, Intl });
    const change = (id, value, event = 'change') => { elements['#' + id].value = value; elements['#' + id].fire(event); };
    return { elements, rows, cards, sorts, change, click: id => elements['#' + id].fire('click'),
        text: id => elements['#' + id].textContent, visible: () => body.children.filter(row => rows.includes(row) && !row.hidden) };
}
const row = (code, name, debit, credit, group = code.slice(0, 3)) => ({ code, name, debit, credit, group });
const data = Array.from({ length: 30 }, (_, i) => row('120.' + (i + 1), 'Müşteri ' + (i + 1), '0.10', '0'));
data.push(row('320.1', 'Tedarikçi Bir', '0', '0.20'), row('320.2', 'Tedarikçi İki', '0', '0.20'),
    row('329.1', 'ÇAMLICA İLAÇ IŞIK', '0', '0.10'), row('399.1', 'Diğer Hesap', '2.50', '0'),
    row('ABC', '<script>test</script>', '0', '0', 'Diger'));
const report = setup(data);
assert.equal(report.visible().length, 25);
assert.equal(report.text('baDebitTotal'), '5,50');
assert.equal(report.text('baCreditTotal'), '0,50');
assert.equal(report.text('baNetTotal'), '-5,00');
report.click('baNext'); assert.equal(report.visible().length, 10);
assert.equal(report.text('baDebitTotal'), '5,50', 'Totals include all matches, not just page');
report.change('baGroup', '120'); assert.equal(report.text('baPageNumber'), '1 / 2');
assert.equal(report.text('baDebitTotal'), '3,00');
report.change('baSearch', 'MUSTERI 29', 'input'); assert.equal(report.visible().length, 1);
report.change('baSearch', '120.29', 'input'); assert.equal(report.visible().length, 1);
report.click('baReset'); assert.equal(report.visible().length, 25);
report.change('baSearch', 'camlica ilac isik', 'input'); assert.equal(report.visible().length, 1);
assert.equal(report.text('baCreditTotal'), '0,10');
report.cards.find(card => card.dataset.group === '320').fire('click');
assert.equal(report.visible().length, 0, 'Card and search filters intersect');
assert.equal(report.elements['#baEmpty'].hidden, false); assert.equal(report.text('baNetTotal'), '0,00');
report.click('baReset'); report.cards.find(card => card.dataset.group === '320').fire('click');
assert.equal(report.visible().length, 2); assert.equal(report.text('baCreditTotal'), '0,40');
assert.equal(report.cards.find(card => card.dataset.group === '320').attributes['aria-pressed'], 'true');
report.click('baReset'); report.change('baPageSize', 'all');
assert.equal(report.visible().length, 35); assert.equal(report.elements['#baNext'].disabled, true);
report.sorts.find(button => button.dataset.sort === 'debit').fire('click');
assert.equal(report.visible().at(-1).dataset.debit, '2.50');
report.sorts.find(button => button.dataset.sort === 'debit').fire('click');
assert.equal(report.visible()[0].dataset.debit, '2.50');
assert.equal(report.sorts.find(button => button.dataset.sort === 'debit').heading.attributes['aria-sort'], 'descending');
report.change('baGroup', 'Diger'); assert.equal(report.visible()[0].dataset.code, 'ABC');
assert.equal(report.visible()[0].dataset.name, '<script>test</script>', 'No HTML interpretation');
const empty = setup([]); assert.equal(empty.text('baDebitTotal'), '0,00');
assert.equal(empty.elements['#baEmpty'].hidden, false); assert.equal(empty.elements['#baNext'].disabled, true);
const precise = setup([row('120.1', 'A', '9007199254740993.10', '0.005'), row('120.2', 'B', '0.20', '0.005')]);
assert.equal(precise.text('baDebitTotal'), '9.007.199.254.740.993,30');
assert.equal(precise.text('baCreditTotal'), '0,01', 'Fractional cents summed before display rounding');
console.log('PASS search, Turkish normalization, group cards, filter intersection/reset, full-result totals, pagination, sorting, empty list, exact decimal arithmetic');

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../..');
const source = fs.readFileSync(path.join(root, 'Ekomers.Web/wwwroot/js/accounting-expense-suggestions.js'), 'utf8');
function element(extra = {}) {
    return { value: '', checked: false, disabled: false, required: false, style: {}, events: {},
        addEventListener(type, callback) { this.events[type] = callback; }, ...extra };
}
const ids = {};
for (const id of ['expenseSuggestionApproval', 'suggestionAccountSearch', 'suggestionSelectVisible', 'suggestionSelectionCount', 'suggestionSearchEmpty', 'suggestionApprove']) ids[id] = element();
const rows = [
    { text: '730.01 FABRİKA KİRA', group: '1' },
    { text: '730.02 ELEKTRİK', group: '' },
    { text: '770.01 OFIS KIRASI', group: '2' }
].map(data => {
    const checkbox = element(); const group = element({ value: data.group });
    return element({ dataset: { suggestionAccount: data.text }, checkbox, group,
        querySelector(selector) { return selector === '[data-suggestion-select]' ? checkbox : group; } });
});
ids.expenseSuggestionApproval.querySelectorAll = () => rows;
let confirmed = true;
vm.runInNewContext(source, { document: { getElementById(id) { return ids[id]; } }, window: { confirm() { return confirmed; } } });
assert.equal(ids.suggestionApprove.disabled, true);
assert.ok(rows.every(row => !row.checkbox.checked));
ids.suggestionSelectVisible.checked = true;
ids.suggestionSelectVisible.events.change();
assert.deepEqual(rows.map(row => row.checkbox.checked), [true, false, true]);
assert.equal(ids.suggestionSelectionCount.textContent, '2 hesap seçildi');
assert.equal(rows[0].group.required, true);
ids.suggestionAccountSearch.value = 'fabrika kira';
ids.suggestionAccountSearch.events.input();
assert.deepEqual(rows.map(row => row.style.display !== 'none'), [true, false, false]);
assert.deepEqual(rows.map(row => row.checkbox.checked), [true, false, false]);
assert.equal(ids.suggestionSelectionCount.textContent, '1 hesap seçildi');
ids.suggestionAccountSearch.value = 'elektrik';
ids.suggestionAccountSearch.events.input();
assert.equal(ids.suggestionApprove.disabled, true);
rows[1].group.value = '3'; rows[1].group.events.change();
ids.suggestionSelectVisible.checked = true; ids.suggestionSelectVisible.events.change();
assert.equal(rows[1].checkbox.checked, true);
rows[1].group.value = ''; rows[1].group.events.change();
let prevented = false;
ids.expenseSuggestionApproval.events.submit({ preventDefault() { prevented = true; } });
assert.equal(prevented, true);
rows[1].group.value = '3'; rows[1].group.events.change();
confirmed = false; prevented = false;
ids.expenseSuggestionApproval.events.submit({ preventDefault() { prevented = true; } });
assert.equal(prevented, true);
confirmed = true; prevented = false;
ids.expenseSuggestionApproval.events.submit({ preventDefault() { prevented = true; } });
assert.equal(prevented, false);
ids.suggestionAccountSearch.value = 'bulunamayan'; ids.suggestionAccountSearch.events.input();
assert.equal(ids.suggestionSearchEmpty.style.display, '');
assert.equal(ids.suggestionSelectionCount.textContent, '0 hesap seçildi');
vm.runInNewContext(source, { document: { getElementById() { return null; } } });
const view = fs.readFileSync(path.join(root, 'Ekomers.Web/Views/SatisKarlilikGiderleri/Oneriler.cshtml'), 'utf8');
assert.match(view, /asp-action="OnerileriOnayla" method="post"/);
assert.match(view, /name="Rows\[@i\]\.Selected" type="checkbox" value="true" data-suggestion-select/);
assert.doesNotMatch(view, /Html\.Raw/);
console.log('Accounting expense suggestion selection/search/confirmation: all tests passed.');

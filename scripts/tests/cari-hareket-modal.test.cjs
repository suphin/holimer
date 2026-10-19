// Run: node scripts/tests/cari-hareket-modal.test.cjs (no packages or database).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../Ekomers.Web/wwwroot/js/reports/cari-hareket-modal.js'), 'utf8');
class Element {
    constructor() { this.events = {}; this.dataset = {}; this.attributes = {}; this.hidden = true; this.innerHTML = ''; this.textContent = ''; this.isConnected = true;
        this.classList = { remove: key => { this.attributes[key] = false; }, add: key => { this.attributes[key] = true; } }; }
    addEventListener(type, callback) { (this.events[type] ||= []).push(callback); }
    fire(type, target = this) { for (const callback of this.events[type] || []) callback({ target }); }
    setAttribute(name, value) { this.attributes[name] = value; }
    closest() { return this; }
    focus() { this.focused = true; }
}
function setup() {
    const root = new Element(); root.contains = () => true;
    const dialog = new Element(); dialog.open = false;
    const close = new Element(); dialog.querySelector = () => close;
    dialog.showModal = () => { dialog.open = true; };
    dialog.close = () => { dialog.open = false; dialog.fire('close'); };
    const content = new Element(), status = new Element();
    const requests = [];
    vm.runInNewContext(source, { document: { getElementById: id => ({ borcAlacakReport: root, baMovementDialog: dialog, baMovementContent: content, baMovementStatus: status })[id] },
        window: { location: { href: 'https://portal.example/Reports/BorcAlacak' }, alert: () => {} },
        URL, AbortController, fetch: (url, options) => new Promise((resolve, reject) => requests.push({ url, options, resolve, reject })) });
    const open = code => { const trigger = new Element(); trigger.dataset.url = '/Reports/CariHareketler?cariKod=' + encodeURIComponent(code); root.fire('click', trigger); return trigger; };
    return { root, dialog, close, content, status, requests, open };
}
const response = (html, status = 200, marker = 'CariHareketler', redirected = false) => ({ status, ok: status === 200, redirected,
    headers: { get: () => marker }, text: async () => html });
const settle = () => new Promise(resolve => setImmediate(resolve));
(async () => {
    const app = setup();
    const firstTrigger = app.open('320.01 & "X"');
    assert.equal(app.dialog.open, true); assert.equal(app.status.hidden, false);
    assert.equal(app.requests[0].url.searchParams.get('cariKod'), '320.01 & "X"');
    assert.equal(app.requests[0].url.searchParams.get('page'), '1');
    assert.equal(app.requests[0].options.cache, 'no-store'); assert.equal(app.requests[0].options.credentials, 'same-origin');
    app.requests[0].resolve(response('<table>safe partial</table>')); await settle();
    assert.equal(app.content.innerHTML, '<table>safe partial</table>'); assert.equal(app.status.hidden, true);
    assert.equal(app.content.attributes['aria-busy'], 'false');
    const next = new Element(); next.dataset.movementPage = '2'; app.content.fire('click', next);
    assert.equal(app.requests[1].url.searchParams.get('page'), '2'); assert.equal(app.content.innerHTML, '');
    app.close.fire('click'); assert.equal(app.dialog.open, false); assert.equal(firstTrigger.focused, true);
    assert.equal(app.requests[1].options.signal.aborted, true);
    app.open('120.02');
    app.requests[1].resolve(response('OLD ACCOUNT RESPONSE')); await settle();
    assert.equal(app.content.innerHTML, '', 'Late response cannot leak into another account');
    app.requests[2].resolve(response('LOGIN PAGE', 200, '', true)); await settle();
    assert.equal(app.content.innerHTML, ''); assert.match(app.status.textContent, /Oturumunuz/);
    assert.equal(app.status.attributes['is-error'], true);
    app.dialog.close(); app.open('329.01');
    app.requests[3].resolve(response('ERROR PAGE', 500, '')); await settle();
    assert.equal(app.content.innerHTML, ''); assert.match(app.status.textContent, /alınamadı/);
    app.dialog.close(); app.open('329.02');
    app.requests[4].resolve(response('FULL PAGE WITH WRONG MARKER', 200, 'Other')); await settle();
    assert.equal(app.content.innerHTML, '');
    app.dialog.close(); app.open('329.03');
    app.requests[5].reject(new Error('Network failure')); await settle();
    assert.equal(app.status.hidden, false); assert.equal(app.content.attributes['aria-busy'], 'false');
    app.dialog.close(); app.open('329.04');
    const invalid = new Element(); invalid.dataset.movementPage = '-1'; app.content.fire('click', invalid);
    assert.equal(app.requests.length, 7);
    const disabled = new Element(); disabled.dataset.movementPage = '5'; disabled.disabled = true; app.content.fire('click', disabled);
    assert.equal(app.requests.length, 7);
    app.dialog.close();
    console.log('PASS modal open/close, safe URL parameters, pagination, loading, focus restoration, abort/stale responses, authentication/error handling and response marker');
})().catch(error => { console.error(error); process.exitCode = 1; });

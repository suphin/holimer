// Run: node scripts/tests/logo-cari-ekstre-modal.test.cjs (no packages/database).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../Ekomers.Web/wwwroot/js/reports/logo-cari-ekstre.js'), 'utf8');
class Element {
    constructor() {
        this.events = {}; this.dataset = {}; this.attributes = {}; this.hidden = true;
        this.innerHTML = ''; this.textContent = ''; this.isConnected = true; this.value = '';
        this.classList = { remove: key => { this.attributes[key] = false; }, add: key => { this.attributes[key] = true; } };
    }
    addEventListener(type, callback) { (this.events[type] ||= []).push(callback); }
    fire(type, target = this) {
        const event = { target, prevented: false, preventDefault() { this.prevented = true; } };
        for (const callback of this.events[type] || []) callback(event);
        return event;
    }
    setAttribute(name, value) { this.attributes[name] = value; }
    hasAttribute(name) { return Object.hasOwn(this.attributes, name); }
    closest(selector) { return selector.split(',').some(x => this.hasAttribute(x.trim().slice(1, -1))) ? this : null; }
    focus() { this.focused = true; }
}
function setup() {
    const root = new Element(); root.contains = () => true;
    root.dataset = { statementPath: '/LogoCariEkstre/Ekstre', documentPath: '/LogoCariEkstre/BelgeDetay' };
    function modal() {
        const dialog = new Element(), content = new Element(), status = new Element(), close = new Element();
        dialog.open = false; dialog.showModal = () => { dialog.open = true; };
        dialog.close = () => { dialog.open = false; dialog.fire('close'); };
        dialog.querySelector = selector => ({ '[data-cs-content]': content, '[data-cs-status]': status, '[data-cs-close]': close })[selector];
        return { dialog, content, status, close };
    }
    const statement = modal(), detail = modal(), form = new Element();
    form.submits = 0; form.submit = () => form.submits++;
    const company = new Element(), period = new Element(), start = new Element(), end = new Element(), requests = [];
    const elements = { logoCariReport: root, csStatementDialog: statement.dialog, csDocumentDialog: detail.dialog,
        csFilter: form, csCompany: company, csPeriod: period, csStart: start, csEnd: end };
    vm.runInNewContext(source, { document: { getElementById: id => elements[id] },
        window: { location: { href: 'https://portal.example/LogoCariEkstre', origin: 'https://portal.example' } },
        URL, AbortController, fetch: (url, options) => new Promise((resolve, reject) => requests.push({ url, options, resolve, reject })) });
    const link = (attribute, href) => { const e = new Element(); e.setAttribute(attribute, ''); e.href = href; return e; };
    const open = (document = false, href = document ? '/LogoCariEkstre/BelgeDetay?firmNumber=100&periodNumber=1&clientRef=42&movementRef=8&startDate=2026-01-01&endDate=2026-10-20' : '/LogoCariEkstre/Ekstre?firmNumber=100&periodNumber=1&clientRef=42&startDate=2026-01-01&endDate=2026-10-20') => {
        const trigger = link(document ? 'data-cs-document' : 'data-cs-statement', href);
        trigger.event = root.fire('click', trigger); return trigger;
    };
    return { root, statement, detail, form, company, period, start, end, requests, open, link };
}
const response = (html, marker = 'Ekstre', status = 200, redirected = false) => ({ status, ok: status === 200, redirected,
    headers: { get: () => marker }, text: async () => html });
const settle = () => new Promise(resolve => setImmediate(resolve));
(async () => {
    const a = setup(), trigger = a.open();
    assert.equal(trigger.event.prevented, true); assert.equal(a.statement.dialog.open, true);
    assert.equal(a.requests[0].url.searchParams.get('clientRef'), '42');
    assert.equal(a.requests[0].url.searchParams.get('firmNumber'), '100');
    assert.equal(a.requests[0].url.searchParams.get('periodNumber'), '1');
    assert.equal(a.requests[0].options.credentials, 'same-origin'); assert.equal(a.requests[0].options.cache, 'no-store');
    a.requests[0].resolve(response('<table>STATEMENT</table>')); await settle();
    assert.equal(a.statement.content.innerHTML, '<table>STATEMENT</table>'); assert.equal(a.statement.status.hidden, true);
    const docTrigger = a.open(true);
    assert.equal(a.statement.dialog.open, true); assert.equal(a.detail.dialog.open, true);
    a.requests[1].resolve(response('<table>INVOICE</table>', 'Belge')); await settle();
    assert.equal(a.detail.content.innerHTML, '<table>INVOICE</table>');
    const page = a.link('data-cs-page', a.requests[1].url.href + '&page=2'); a.detail.content.fire('click', page);
    assert.equal(a.requests[2].url.searchParams.get('page'), '2'); assert.equal(a.requests[2].url.searchParams.get('movementRef'), '8');
    a.detail.close.fire('click'); assert.equal(docTrigger.focused, true); assert.equal(a.requests[2].options.signal.aborted, true);
    a.requests[2].resolve(response('LATE CLOSED DOCUMENT', 'Belge')); await settle();
    assert.equal(a.detail.content.innerHTML, ''); assert.equal(a.statement.dialog.open, true);
    a.statement.close.fire('click'); assert.equal(trigger.focused, true);
    a.open(); const stale = a.requests.at(-1); a.open(false, '/LogoCariEkstre/Ekstre?firmNumber=101&periodNumber=2&clientRef=42');
    assert.equal(stale.options.signal.aborted, true);
    stale.resolve(response('PREVIOUS COMPANY')); await settle(); assert.equal(a.statement.content.innerHTML, '');
    a.requests.at(-1).resolve(response('CURRENT COMPANY')); await settle(); assert.equal(a.statement.content.innerHTML, 'CURRENT COMPANY');
    for (const [status, marker, redirected, message] of [[401,'Ekstre',false,/Oturumunuz/],[403,'Ekstre',false,/Oturumunuz/],
        [200,'Ekstre',true,/Oturumunuz/],[404,'Ekstre',false,/bulunamadı/],[503,'Ekstre',false,/okunamadı/],[200,'Belge',false,/okunamadı/]]) {
        a.open(); a.requests.at(-1).resolve(response('DO NOT INSERT', marker, status, redirected)); await settle();
        assert.equal(a.statement.content.innerHTML, ''); assert.match(a.statement.status.textContent, message);
        assert.equal(a.statement.content.attributes['aria-busy'], 'false');
    }
    const count = a.requests.length;
    a.open(false, 'https://evil.example/LogoCariEkstre/Ekstre'); await settle();
    a.open(false, '/Home/SignIn'); await settle(); assert.equal(a.requests.length, count);
    assert.match(a.statement.status.textContent, /Geçersiz/);
    a.statement.dialog.close(); a.statement.dialog.showModal = undefined;
    assert.equal(a.open().event.prevented, false, 'Native hyperlink remains usable without dialog support');
    a.period.value = '2'; a.start.value = '2026-01-01'; a.end.value = '2026-10-20'; a.company.fire('change');
    assert.equal(a.period.value, ''); assert.equal(a.start.value, ''); assert.equal(a.end.value, ''); assert.equal(a.form.submits, 1);
    a.period.value = '1'; a.start.value = '2026-01-01'; a.period.fire('change');
    assert.equal(a.period.value, '1'); assert.equal(a.start.value, ''); assert.equal(a.form.submits, 2);
    console.log('PASS nested modals, scoped URLs, document pagination, focus, abort/stale responses, authentication/error markers, URL guard, link fallback and company/period filter reset');
})().catch(error => { console.error(error); process.exitCode = 1; });

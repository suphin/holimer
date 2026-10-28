(() => {
    'use strict';
    const root = document.getElementById('pivotApp');
    if (!root) return;
    const edit = root.dataset.mode === 'edit', builder = root.querySelector('#pvBuilder');
    if (!builder || !root.querySelector('#pvBootstrap')) return;
    const boot = JSON.parse(root.querySelector('#pvBootstrap').textContent), status = root.querySelector('#pvStatus');
    let columns = boot.columns || [], settings = boot.settings || {}, request, revision = 0;
    if (edit) { try { settings = JSON.parse(boot.settingsJson); } catch { settings = {}; } }
    // Existing saved JSON may use .NET property casing; never render JSON using innerHTML.
    settings = { allowedFields: settings.allowedFields || settings.AllowedFields || [], layout: settings.layout || settings.Layout || {} };
    const normalize = l => ({ rows: l.rows || l.Rows || [], column: l.column ?? l.Column ?? '', measures: (l.measures || l.Measures || []).map(m => ({ field: m.field || m.Field, function: m.function || m.Function })), filters: (l.filters || l.Filters || []).map(f => ({ field: f.field || f.Field, operator: f.operator || f.Operator, value: f.value ?? f.Value ?? '' })) });
    settings.layout = normalize(settings.layout);
    const defaults = JSON.parse(JSON.stringify(settings.layout));
    function node(tag, text, cls) { const n = document.createElement(tag); if (text !== undefined) n.textContent = text; if (cls) n.className = cls; return n; }
    function message(text, error = false) { status.textContent = text; status.className = error ? 'pv-error' : 'pv-ok'; }
    function invalidate() { revision++; request?.abort(); root.removeAttribute('aria-busy'); const out = root.querySelector('#pvOutput'); if (out) out.replaceChildren(); }
    function select(options, chosen, multiple = false) {
        const s = node('select', undefined, 'form-control'); s.multiple = multiple;
        for (const [value, text] of options) { const o = node('option', text); o.value = value; o.selected = multiple ? chosen.includes(value) : value === chosen; s.append(o); }
        return s;
    }
    function fieldOptions(empty = true) { return (empty ? [['', 'Seçiniz / Yok']] : []).concat(columns.filter(c => settings.allowedFields.includes(c.name)).map(c => [c.name, c.name + (c.date ? ' · Tarih' : c.numeric ? ' · Sayı' : '')])); }
    function labeled(parent, text, control) { const box = node('div'), label = node('label', text); const id = 'pv-' + (++labeled.next); control.id = id; label.htmlFor = id; box.append(label, control); parent.append(box); return control; } labeled.next = 0;
    function readLayout() {
        return { rows: [...builder.querySelector('[data-rows]').selectedOptions].map(o => o.value), column: builder.querySelector('[data-column]').value,
            measures: [...builder.querySelectorAll('[data-measure]')].map(b => ({ field: b.querySelector('[data-field]').value, function: b.querySelector('[data-function]').value })).filter(m => m.field),
            filters: [...builder.querySelectorAll('[data-filter]')].map(b => ({ field: b.querySelector('select').value, operator: b.querySelectorAll('select')[1].value, value: b.querySelector('input').value })).filter(f => f.field) };
    }
    function renderBuilder(layout) {
        builder.replaceChildren(); builder.className = 'pv-builder';
        const axes = node('div', undefined, 'pv-grid pv-axes');
        const rs = labeled(axes, 'Satır Alanları (en fazla 3)', select(fieldOptions(false), layout.rows, true)); rs.dataset.rows = '';
        const cs = labeled(axes, 'Sütun Alanı (isteğe bağlı)', select(fieldOptions(), layout.column)); cs.dataset.column = '';
        const help = node('p', 'Çoklu seçim: Ctrl / Command tuşu. Alanlar kaynak sütun sırasıyla gösterilir.', 'pv-help');
        const measures = node('div', undefined, 'pv-measures');
        for (let i = 0; i < 3; i++) {
            const m = layout.measures[i] || {}, box = node('div', undefined, 'pv-measure'); box.dataset.measure = '';
            const field = labeled(box, 'Değer ' + (i + 1), select(fieldOptions(), m.field || '')); field.dataset.field = '';
            const fn = labeled(box, 'Hesaplama', select([['sum', 'Toplam'], ['count', 'Dolu Kayıt Sayısı'], ['avg', 'Ortalama'], ['min', 'En Az'], ['max', 'En Çok']], m.function || 'sum')); fn.dataset.function = '';
            const restrict = () => { const c = columns.find(c => c.name === field.value); for (const o of fn.options) o.disabled = !!c && !c.numeric && o.value !== 'count'; if (c && !c.numeric) fn.value = 'count'; };
            field.addEventListener('change', restrict); restrict(); measures.append(box);
        }
        const details = node('details'), summary = node('summary', 'Filtreler · En fazla 5 koşul'); details.append(summary, node('p', 'Aynı alanın Eşittir seçenekleri VEYA ile, farklı alanlar ve diğer koşullar VE ile birleşir. Örnek: Yıl = 2026 VE (Fatura Türü = Perakende VEYA Toptan). Tarih/tutar aralıkları VE olarak kalır. Boş alanlı satırlar kullanılmaz. Sayılarda ondalık nokta (1234.50), tarihlerde yyyy-AA-gg kullanın; tarihler gün bazındadır.', 'pv-help'));
        for (let i = 0; i < 5; i++) {
            const f = layout.filters[i] || {}, box = node('div', undefined, 'pv-filter'); box.dataset.filter = '';
            const field = labeled(box, 'Filtre Alanı ' + (i + 1), select(fieldOptions(), f.field || ''));
            const op = labeled(box, 'Koşul', select([['eq', 'Eşittir'], ['contains', 'İçerir'], ['gte', 'Büyük / Eşit'], ['lte', 'Küçük / Eşit']], f.operator || 'eq'));
            const input = node('input', undefined, 'form-control'); input.value = f.value || ''; input.maxLength = 250; labeled(box, 'Değer', input);
            const filterType = () => { const c = columns.find(c => c.name === field.value); input.type = c?.date ? 'date' : 'text'; if (c?.date) input.min = '1753-01-01'; const contains = [...op.options].find(o => o.value === 'contains'); contains.disabled = !!c && !['char','varchar','nchar','nvarchar'].includes(c.dataType); if (contains.disabled && op.value === 'contains') op.value = 'eq'; };
            field.addEventListener('change', filterType); filterType(); details.append(box);
        }
        if (layout.filters.length) details.open = true;
        builder.append(axes, help, measures, details);
    }
    async function jsonFetch(url, options) {
        const parsed = new URL(url, location.href); if (parsed.origin !== location.origin) throw new Error('Geçersiz rapor adresi.');
        const response = await fetch(parsed, { ...options, credentials: 'same-origin', redirect: 'error', headers: { ...options?.headers, Accept: 'application/json' } });
        if (!response.headers.get('content-type')?.includes('application/json')) throw new Error('Oturum veya yetki kontrolü başarısız. Sayfayı yenileyip tekrar giriş yapınız.');
        const data = await response.json(); if (!response.ok) throw new Error(data.error || 'Rapor alınamadı. Yetkiyi ve bağlantıyı kontrol ediniz.'); return data;
    }
    builder.addEventListener('change', () => { invalidate(); if (!edit) message('Seçimler değişti. Pivotu yeniden hesaplayınız.'); });
    builder.addEventListener('input', () => { invalidate(); });
    if (edit) {
        const save = root.querySelector('#pvSave'), schema = root.querySelector('#pvSchema'), view = root.querySelector('#pvView');
        let loadedSource = '';
        const sourceKey = () => JSON.stringify([schema.value, view.value]);
        for (const f of [schema, view]) f.addEventListener('input', () => { invalidate(); save.disabled = true; loadedSource = ''; message('Kaynak değişti. View alanlarını tekrar getiriniz.'); });
        const allowedBox = root.querySelector('#pvAllowed');
        root.querySelector('#pvLoad').addEventListener('click', async () => {
            invalidate(); const current = revision; request = new AbortController(); save.disabled = true; root.setAttribute('aria-busy', 'true'); message('View alanları okunuyor…');
            try {
                const url = new URL(root.dataset.columnsUrl, location.href); url.searchParams.set('schema', schema.value); url.searchParams.set('view', view.value);
                const data = await jsonFetch(url, { signal: request.signal }); if (current !== revision) return;
                columns = data; allowedBox.replaceChildren();
                if (!settings.allowedFields.length) settings.allowedFields = columns.map(c => c.name).slice(0, 100);
                for (const c of columns) { const label = node('label'), cb = node('input'); cb.type = 'checkbox'; cb.value = c.name; cb.checked = settings.allowedFields.includes(c.name); label.append(cb, document.createTextNode(' ' + c.name), node('small', c.dataType)); allowedBox.append(label); }
                settings.allowedFields = [...allowedBox.querySelectorAll('input:checked')].map(c => c.value);
                const layout = settings.layout;
                if (!layout.measures.length) { const c = columns.find(c => c.numeric && settings.allowedFields.includes(c.name)) || columns[0]; if (c) layout.measures = [{ field: c.name, function: c.numeric ? 'sum' : 'count' }]; }
                if (!layout.rows.length) { const c = columns.find(c => !c.numeric && settings.allowedFields.includes(c.name)); if (c) layout.rows = [c.name]; }
                renderBuilder(layout); loadedSource = sourceKey(); save.disabled = false; message(columns.length + ' desteklenen alan bulundu. Kullanılabilir alanları ve başlangıç düzenini kontrol ediniz.');
            } catch (e) { if (e.name !== 'AbortError' && current === revision) message(e.message, true); }
            finally { if (current === revision) root.removeAttribute('aria-busy'); }
        });
        allowedBox.addEventListener('change', () => { const layout = readLayout(); settings.allowedFields = [...allowedBox.querySelectorAll('input:checked')].map(c => c.value); invalidate(); renderBuilder(layout); });
        root.querySelector('#pvEditForm').addEventListener('submit', e => {
            if (!loadedSource || loadedSource !== sourceKey()) { e.preventDefault(); message('Önce kaynak view alanlarını getiriniz.', true); return; }
            const layout = readLayout(); if (!layout.measures.length || layout.rows.length > 3 || !settings.allowedFields.length || settings.allowedFields.length > 100) { e.preventDefault(); message('1–100 alan, en fazla 3 satır alanı ve en az bir değer seçiniz.', true); return; }
            root.querySelector('#pvSettings').value = JSON.stringify({ allowedFields: settings.allowedFields, layout });
        });
        // Explicit load lets administrators review schema changes before saving.
        message('LogoConnection view alanlarını getirmek için yukarıdaki butonu kullanınız.');
    } else {
        if (!columns.length) return;
        renderBuilder(settings.layout);
        root.querySelector('#pvReset').addEventListener('click', () => { invalidate(); renderBuilder(defaults); message('Varsayılan düzen yüklendi. Pivotu hesaplayınız.'); });
        root.querySelector('#pvRun').addEventListener('click', async () => {
            const layout = readLayout(); invalidate(); const current = revision;
            if (!layout.measures.length || layout.rows.length > 3) { message('En az bir değer ve en fazla 3 satır alanı seçiniz.', true); return; }
            request = new AbortController(); root.setAttribute('aria-busy', 'true'); message('Pivot hesaplanıyor…');
            try {
                const data = await jsonFetch(root.dataset.runUrl, { method: 'POST', signal: request.signal, headers: { 'Content-Type': 'application/json', RequestVerificationToken: root.querySelector('input[name="__RequestVerificationToken"]').value }, body: JSON.stringify(layout) });
                if (current !== revision) return; renderResult(data, layout); message(data.sourceRowCount.toLocaleString('tr-TR') + ' kaynak kaydı · ' + data.cells.length + ' dolu pivot hücresi. Toplamlar tüm filtrelenmiş veriyi kapsar.');
            } catch (e) { if (e.name !== 'AbortError' && current === revision) message(e.message, true); }
            finally { if (current === revision) root.removeAttribute('aria-busy'); }
        });
    }
    function renderResult(data, layout) {
        const output = root.querySelector('#pvOutput'); output.replaceChildren();
        if (!data.cells.length) { output.append(node('p', 'Seçilen filtrelerde kayıt bulunamadı.', 'alert alert-info')); return; }
        const format = n => n == null ? '—' : new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 6 }).format(n);
        const label = s => s === null ? '(Boş / NULL)' : s === '' ? '(Boş metin)' : s;
        const key = JSON.stringify, compare = (a, b) => a.localeCompare(b, 'tr', { numeric: true });
        const rows = [...data.rowTotals].sort((a, b) => compare(key(a.row), key(b.row))), cols = [...data.columnTotals].sort((a, b) => compare(key(a.column), key(b.column)));
        const cells = new Map(data.cells.map(c => [key([c.row, c.column]), c.values]));
        const table = node('table'), head = node('thead'), tr = node('tr');
        const first = node('th', layout.rows.join(' / ') || 'Tümü'); first.scope = 'col'; tr.append(first);
        for (const c of cols) for (const h of data.headings) { const th = node('th', (layout.column ? label(c.column) + ' · ' : '') + h); th.scope = 'col'; tr.append(th); }
        if (layout.column) for (const h of data.headings) tr.append(node('th', 'Satır Toplamı · ' + h));
        head.append(tr); const body = node('tbody');
        const appendValues = (tr, values, cls) => { for (let i = 0; i < data.headings.length; i++) tr.append(node('td', format(values?.[i]), cls)); };
        for (const row of rows) { const tr = node('tr'), th = node('th', row.row.map(label).join(' / ') || 'Tümü'); th.scope = 'row'; tr.append(th); for (const c of cols) appendValues(tr, cells.get(key([row.row, c.column]))); if (layout.column) appendValues(tr, row.values, 'pv-total'); body.append(tr); }
        const foot = node('tfoot'), total = node('tr'); total.append(node('th', 'Genel Toplam')); for (const c of cols) appendValues(total, c.values); if (layout.column) appendValues(total, data.grandTotal); foot.append(total);
        const caption = node('caption', 'Pivot tablo · ' + data.sourceRowCount.toLocaleString('tr-TR') + ' kaynak kaydı. Ortalama toplamları dolu kayıt sayısına göre ağırlıklıdır.');
        table.append(caption, head, body, foot); const wrap = node('div', undefined, 'pv-table-wrap'); wrap.tabIndex = 0; wrap.setAttribute('role', 'region'); wrap.setAttribute('aria-label', 'Pivot sonuçları'); wrap.append(table); const box = node('div', undefined, 'pv-result'); box.append(wrap); output.append(box);
    }
})();

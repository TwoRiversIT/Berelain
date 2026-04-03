/**
 * Font Awesome Icon Picker — TwoRivers.Berelain
 *
 * Initializes one picker instance per [data-fa-field-id] element on the page.
 * Uses Bootstrap 5 modal (data-bs-toggle wires open; JS wires content behavior).
 * Icon lists are fetched lazily from the curated-icons endpoint and cached by URL.
 */
(function () {
    'use strict';

    // Shared promise cache keyed by API URL. Shared across all field instances so
    // the network request fires at most once per unique endpoint per page load.
    const iconCache = new Map();

    function resolveFontDefinition(icon) {
        if (!icon) {
            return null;
        }

        const family = icon.family || '';
        const style = icon.style || '';

        switch (family) {
            case 'classic':
                return {
                    fontFamily: '"Font Awesome 7 Pro"',
                    fontWeight: style === 'thin' ? '100' : style === 'light' ? '300' : style === 'regular' ? '400' : '900',
                };
            case 'brands':
                return {
                    fontFamily: '"Font Awesome 7 Brands"',
                    fontWeight: '400',
                };
            case 'sharp':
                return {
                    fontFamily: '"Font Awesome 7 Sharp"',
                    fontWeight: style === 'thin' ? '100' : style === 'light' ? '300' : style === 'regular' ? '400' : '900',
                };
            case 'duotone':
                return {
                    fontFamily: '"Font Awesome 7 Duotone"',
                    fontWeight: style === 'thin' ? '100' : style === 'light' ? '300' : style === 'regular' ? '400' : '900',
                };
            case 'sharp-duotone':
                return {
                    fontFamily: '"Font Awesome 7 Sharp Duotone"',
                    fontWeight: style === 'thin' ? '100' : style === 'light' ? '300' : style === 'regular' ? '400' : '900',
                };
            default:
                return null;
        }
    }

    function setIconGlyph(element, icon) {
        if (!element) {
            return;
        }

        if (!icon || !icon.unicode) {
            element.style.removeProperty('--fa-picker-content');
            element.style.removeProperty('--fa-picker-font-family');
            element.style.removeProperty('--fa-picker-font-weight');
            element.dataset.iconKey = '';
            return;
        }

        const font = resolveFontDefinition(icon);
        const unicode = String(icon.unicode).replace(/^\\+/, '').trim();
        element.style.setProperty('--fa-picker-content', '"\\' + unicode + ' "');
        element.style.setProperty('--fa-picker-font-family', font?.fontFamily ?? '"Font Awesome 7 Pro"');
        element.style.setProperty('--fa-picker-font-weight', font?.fontWeight ?? '400');
        element.dataset.iconKey = icon.key || '';
    }

    /**
     * Returns a Promise that resolves to the icon array from the given API URL.
     * The result is cached so subsequent calls return the same Promise without
     * issuing a second request.
     */
    function fetchIcons(apiUrl) {
        if (iconCache.has(apiUrl)) {
            return iconCache.get(apiUrl);
        }

        const promise = fetch(apiUrl, {
            method: 'GET',
            headers: { Accept: 'application/json' },
            credentials: 'same-origin',
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Request failed (${response.status})`);
                }
                return response.json();
            })
            .then(payload => (Array.isArray(payload?.icons) ? payload.icons : []));

        iconCache.set(apiUrl, promise);
        return promise;
    }

    /** Returns a filtered + capped slice of the icon array matching the search query. */
    function filterIcons(icons, query) {
        const q = (query ?? '').trim().toLowerCase();

        if (!q) {
            return icons.slice(0, 150);
        }

        const result = [];
        for (const icon of icons) {
            if (result.length >= 150) {
                break;
            }

            const haystack = [
                icon.key,
                icon.label,
                icon.name,
                ...(Array.isArray(icon.searchTerms) ? icon.searchTerms : []),
            ]
                .join(' ')
                .toLowerCase();

            if (haystack.includes(q)) {
                result.push(icon);
            }
        }

        return result;
    }

    /** Wires up a single field instance. Safe to call multiple times — idempotent. */
    function initField(fieldEl) {
        if (fieldEl.dataset.faPickerInit === 'true') {
            return;
        }
        fieldEl.dataset.faPickerInit = 'true';

        const apiUrl = fieldEl.dataset.faApiUrl;
        const fieldId = fieldEl.dataset.faFieldId;

        const hiddenInput = fieldEl.querySelector('.fa-icon-key-input');
        const previewGlyph = fieldEl.querySelector('.fa-icon-preview-glyph');
        const keyDisplay = fieldEl.querySelector('.fa-icon-key-display');
        const closedClearBtn = fieldEl.querySelector('.fa-icon-closed-clear');

        const modal = document.getElementById('fa-picker-modal-' + fieldId);

        if (!modal || !hiddenInput) {
            return;
        }

        const searchInput = modal.querySelector('.fa-picker-search');
        const statusEl = modal.querySelector('.fa-picker-status');
        const resultsEl = modal.querySelector('.fa-picker-results');
        const selectionGlyph = modal.querySelector('.fa-picker-selection-glyph');
        const selectionKey = modal.querySelector('.fa-picker-selection-key');
        const modalClearBtn = modal.querySelector('.fa-picker-modal-clear');
        const applyBtn = modal.querySelector('.fa-picker-apply');

        let allIcons = [];
        let iconIndex = new Map();
        let filtered = [];
        let activeIndex = -1;
        let pendingKey = '';

        function setIcons(icons) {
            allIcons = Array.isArray(icons) ? icons : [];
            iconIndex = new Map(allIcons.map(icon => [icon.key, icon]));
        }

        function findIcon(key) {
            return key ? (iconIndex.get(key) ?? null) : null;
        }

        // ── Closed-state helpers ─────────────────────────────────────────────────

        function applyClosedState(iconKey) {
            const key = iconKey || '';
            hiddenInput.value = key;
            hiddenInput.dispatchEvent(new Event('change', { bubbles: true }));
            keyDisplay.textContent = key;

            if (key) {
                setIconGlyph(previewGlyph, findIcon(key));
                closedClearBtn?.classList.remove('d-none');
            } else {
                setIconGlyph(previewGlyph, null);
                closedClearBtn?.classList.add('d-none');
            }
        }

        // ── Modal-state helpers ──────────────────────────────────────────────────

        function setModalSelection(iconKey) {
            pendingKey = iconKey || '';
            selectionKey.textContent = pendingKey;

            if (pendingKey) {
                setIconGlyph(selectionGlyph, findIcon(pendingKey));
                modalClearBtn?.classList.remove('d-none');
            } else {
                setIconGlyph(selectionGlyph, null);
                modalClearBtn?.classList.add('d-none');
            }

            highlightActive();
        }

        function highlightActive() {
            resultsEl.querySelectorAll('.fa-picker-item').forEach(btn => {
                btn.classList.toggle('fa-picker-item--selected', btn.dataset.iconKey === pendingKey);
            });
            activeIndex = filtered.findIndex(icon => icon.key === pendingKey);
        }

        function scrollActiveIntoView() {
            resultsEl.querySelector('.fa-picker-item--selected')?.scrollIntoView({ block: 'nearest' });
        }

        function renderResults() {
            filtered = filterIcons(allIcons, searchInput.value);
            resultsEl.innerHTML = '';

            if (filtered.length === 0) {
                statusEl.textContent = 'No matching icons.';
                return;
            }

            const count = filtered.length;
            statusEl.textContent = `${count} icon${count === 1 ? '' : 's'} shown. Arrow keys to navigate, Enter to select.`;

            const fragment = document.createDocumentFragment();

            for (const icon of filtered) {
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'fa-picker-item';
                btn.dataset.iconKey = icon.key;
                btn.setAttribute('role', 'option');

                const iconEl = document.createElement('span');
                iconEl.className = 'fa-picker-item-icon fa-admin-icon-glyph';
                setIconGlyph(iconEl, icon);
                iconEl.setAttribute('aria-hidden', 'true');

                const labelEl = document.createElement('span');
                labelEl.className = 'fa-picker-item-label';
                labelEl.textContent = icon.label || icon.name;

                const keyEl = document.createElement('small');
                keyEl.className = 'fa-picker-item-key';
                keyEl.textContent = icon.key;

                btn.append(iconEl, labelEl, keyEl);
                btn.addEventListener('click', () => setModalSelection(icon.key));
                fragment.appendChild(btn);
            }

            resultsEl.appendChild(fragment);
            highlightActive();
            scrollActiveIntoView();
        }

        function navigateTo(index) {
            const items = resultsEl.querySelectorAll('.fa-picker-item');
            if (items.length === 0) {
                activeIndex = -1;
                return;
            }

            if (index < 0) {
                index = items.length - 1;
            } else if (index >= items.length) {
                index = 0;
            }

            activeIndex = index;
            items.forEach(item => item.classList.remove('fa-picker-item--selected'));
            items[activeIndex].classList.add('fa-picker-item--selected');
            items[activeIndex].scrollIntoView({ block: 'nearest' });
            pendingKey = filtered[activeIndex]?.key ?? '';
            selectionKey.textContent = pendingKey;

            if (pendingKey) {
                setIconGlyph(selectionGlyph, findIcon(pendingKey));
                modalClearBtn?.classList.remove('d-none');
            } else {
                setIconGlyph(selectionGlyph, null);
                modalClearBtn?.classList.add('d-none');
            }
        }

        // ── Modal events ─────────────────────────────────────────────────────────

        modal.addEventListener('show.bs.modal', () => {
            pendingKey = hiddenInput.value || '';
            searchInput.value = '';

            if (allIcons.length > 0) {
                setModalSelection(pendingKey);
                statusEl.textContent = '';
                renderResults();
                return;
            }

            statusEl.textContent = 'Loading curated icons\u2026';
            resultsEl.innerHTML = '';
            setIconGlyph(selectionGlyph, null);
            selectionKey.textContent = pendingKey;

            fetchIcons(apiUrl)
                .then(icons => {
                    setIcons(icons);
                    setModalSelection(pendingKey);
                    renderResults();
                })
                .catch(err => {
                    statusEl.textContent = `Unable to load icons: ${err.message}`;
                });
        });

        modal.addEventListener('shown.bs.modal', () => {
            searchInput.focus();
        });

        // ── Search input events ───────────────────────────────────────────────────

        searchInput.addEventListener('input', renderResults);

        searchInput.addEventListener('keydown', e => {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                navigateTo(activeIndex + 1);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                navigateTo(activeIndex - 1);
            } else if (e.key === 'Enter' && activeIndex >= 0 && filtered[activeIndex]) {
                e.preventDefault();
                setModalSelection(filtered[activeIndex].key);
            }
        });

        // ── Button events ─────────────────────────────────────────────────────────

        applyBtn?.addEventListener('click', () => {
            applyClosedState(pendingKey);
            bootstrap.Modal.getInstance(modal)?.hide();
        });

        modalClearBtn?.addEventListener('click', () => {
            setModalSelection('');
        });

        closedClearBtn?.addEventListener('click', () => {
            applyClosedState('');
        });

        fetchIcons(apiUrl)
            .then(icons => {
                setIcons(icons);
                applyClosedState(hiddenInput.value || '');
            })
            .catch(() => {
                // Leave previews blank if the icon catalog is unavailable.
            });
    }

    // ── Bootstrap availability guard ─────────────────────────────────────────────

    function init() {
        if (typeof bootstrap === 'undefined' || typeof bootstrap.Modal === 'undefined') {
            // Bootstrap not yet loaded — defer until it becomes available.
            window.addEventListener('load', init, { once: true });
            return;
        }

        document.querySelectorAll('[data-fa-field-id]').forEach(initField);
    }

    // ── DOM-ready initialization ──────────────────────────────────────────────────

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // ── MutationObserver for dynamically added fields ─────────────────────────────
    // Covers Orchard list/bag field scenarios where new inputs are injected at runtime.

    new MutationObserver(mutations => {
        for (const m of mutations) {
            for (const node of m.addedNodes) {
                if (node.nodeType !== Node.ELEMENT_NODE) {
                    continue;
                }

                if ('faFieldId' in (node.dataset ?? {})) {
                    initField(node);
                }

                node.querySelectorAll?.('[data-fa-field-id]').forEach(initField);
            }
        }
    }).observe(document.body, { childList: true, subtree: true });
})();

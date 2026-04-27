// Setlist Management
//
// Items are tagged with a `kind` discriminator:
//   { kind: 'chorus', id, name, key, type, timeSignature }
//   { kind: 'verse',  bookId, bookName, chapter, verse, text, ref }
// Legacy items (no kind field) are normalized to 'chorus' on load so
// existing saved setlists keep working.
class SetlistManager {
    constructor() {
        this.setlist = [];
        // Index of the item currently being shown via openItem(). Used by
        // advance()/retreat() so prev/next chips on either surface (chorus
        // iframe or Bible overlay) walk the same mixed-kind list.
        this.runnerIndex = -1;
        // Debounced auto-save handle. Every mutation calls
        // saveToLocalStorage() which schedules a server-side flush of the
        // current setlist as the user's "working draft" (per-user, single
        // record, replaces all items atomically).
        this._autoSaveTimer = null;
        this._suppressAutoSave = false;
        this.loadFromLocalStorage();
        this.initializeEventListeners();
        this.listenForVerseAdds();
        // Async, no await: pulls the user's server-side working draft
        // and overlays it onto localStorage so the setlist roams between
        // sessions/devices.
        this._loadWorkingDraft();
    }

    // ---------- item identity ----------
    static keyOf(item) {
        if (!item) return '';
        if (item.kind === 'verse') return `verse:${item.bookId}:${item.chapter}:${item.verse}`;
        return `chorus:${item.id}`;
    }
    static normalizeItem(raw) {
        if (!raw || typeof raw !== 'object') return null;
        if (raw.kind === 'verse') {
            if (!raw.bookId || !raw.chapter || !raw.verse) return null;
            return {
                kind: 'verse',
                bookId: String(raw.bookId),
                bookName: String(raw.bookName || raw.bookId),
                chapter: Number(raw.chapter),
                verse: Number(raw.verse),
                text: String(raw.text || ''),
                ref: String(raw.ref || `${raw.bookName || raw.bookId} ${raw.chapter}:${raw.verse}`),
            };
        }
        // Default: chorus (covers both kind:'chorus' and legacy items without kind)
        if (!raw.id) return null;
        return {
            kind: 'chorus',
            id: raw.id,
            name: raw.name,
            key: raw.key,
            type: raw.type,
            timeSignature: raw.timeSignature,
        };
    }

    // ---------- persistence ----------
    loadFromLocalStorage() {
        try {
            const saved = localStorage.getItem('chap2_setlist');
            if (!saved) return;
            const parsed = JSON.parse(saved);
            if (!Array.isArray(parsed)) return;
            this.setlist = parsed.map(SetlistManager.normalizeItem).filter(Boolean);
            debug(`Loaded ${this.setlist.length} setlist items from localStorage`);
        } catch (e) {
            console.error('Error loading setlist from localStorage:', e);
            this.setlist = [];
        }
    }
    saveToLocalStorage() {
        try {
            localStorage.setItem('chap2_setlist', JSON.stringify(this.setlist));
            debug(`Saved ${this.setlist.length} setlist items to localStorage`);
        } catch (e) {
            console.error('Error saving setlist to localStorage:', e);
        }
        if (!this._suppressAutoSave) this._scheduleAutoSave();
    }

    // ---------- working draft (per-user, server-side, auto) ----------
    _scheduleAutoSave() {
        if (this._autoSaveTimer) clearTimeout(this._autoSaveTimer);
        this._autoSaveTimer = setTimeout(() => this._autoSaveNow(), 800);
    }

    async _autoSaveNow() {
        this._autoSaveTimer = null;
        const payload = {
            items: this.setlist.map(SetlistManager.toServerPayload).filter(Boolean),
        };
        try {
            await fetch('/Setlists/SaveWorking', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload),
            });
        } catch (e) {
            console.warn('Working draft auto-save failed', e);
        }
    }

    async _loadWorkingDraft() {
        try {
            const response = await fetch('/Setlists/Working', {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin',
            });
            // 204 = user has no saved draft yet. If we have local items,
            // push them up so other devices pick them up next time.
            if (response.status === 204) {
                if (this.setlist.length > 0) this._scheduleAutoSave();
                return;
            }
            if (!response.ok) return;
            const setlist = await response.json();
            const items = (setlist.items || []).map(SetlistManager.fromServerItem).filter(Boolean);
            if (items.length === 0) {
                if (this.setlist.length > 0) this._scheduleAutoSave();
                return;
            }
            // Server is source of truth — replace local. Suppress the
            // auto-save round-trip (we just got these from the server).
            this.setlist = items;
            this.runnerIndex = -1;
            this._suppressAutoSave = true;
            try {
                localStorage.setItem('chap2_setlist', JSON.stringify(this.setlist));
            } catch (_) { /* quota/private mode — ignore */ }
            this._suppressAutoSave = false;
            this.refreshDisplay();
        } catch (e) {
            console.warn('Working draft load failed', e);
        }
    }

    // ---------- mutation ----------
    addChorus(chorus) {
        const item = SetlistManager.normalizeItem({ ...chorus, kind: 'chorus' });
        if (!item) return false;
        const key = SetlistManager.keyOf(item);
        if (this.setlist.some(i => SetlistManager.keyOf(i) === key)) {
            this.showNotification('Chorus already in setlist', 'warning');
            return false;
        }
        this.setlist.push(item);
        this.saveToLocalStorage();
        this.refreshDisplay();
        this.showNotification(`Added "${item.name}" to setlist`, 'success');
        return true;
    }
    addVerse(verse) {
        const item = SetlistManager.normalizeItem({ ...verse, kind: 'verse' });
        if (!item) return false;
        const key = SetlistManager.keyOf(item);
        if (this.setlist.some(i => SetlistManager.keyOf(i) === key)) {
            this.showNotification(`${item.ref} already in setlist`, 'warning');
            return false;
        }
        this.setlist.push(item);
        this.saveToLocalStorage();
        this.refreshDisplay();
        this.showNotification(`Added ${item.ref} to setlist`, 'success');
        return true;
    }

    // Remove by composite key (works for both kinds).
    removeItem(key) {
        const item = this.setlist.find(i => SetlistManager.keyOf(i) === key);
        this.setlist = this.setlist.filter(i => SetlistManager.keyOf(i) !== key);
        this.saveToLocalStorage();
        this.refreshDisplay();
        if (item) {
            const label = item.kind === 'verse' ? item.ref : item.name;
            this.showNotification(`Removed ${label} from setlist`, 'info');
        }
    }
    // Backwards-compat shim used by older inline handlers in case any
    // remain in cached HTML.
    removeChorus(chorusId) { this.removeItem(`chorus:${chorusId}`); }

    moveUp(key) {
        const index = this.setlist.findIndex(i => SetlistManager.keyOf(i) === key);
        if (index > 0) {
            [this.setlist[index], this.setlist[index - 1]] = [this.setlist[index - 1], this.setlist[index]];
            this.saveToLocalStorage();
            this.refreshDisplay();
        }
    }
    moveDown(key) {
        const index = this.setlist.findIndex(i => SetlistManager.keyOf(i) === key);
        if (index >= 0 && index < this.setlist.length - 1) {
            [this.setlist[index], this.setlist[index + 1]] = [this.setlist[index + 1], this.setlist[index]];
            this.saveToLocalStorage();
            this.refreshDisplay();
        }
    }

    clearAll() {
        if (this.setlist.length === 0) return;
        if (confirm(`Are you sure you want to remove all ${this.setlist.length} items from your setlist?`)) {
            this.setlist = [];
            this.saveToLocalStorage();
            localStorage.removeItem('chap2_setlist_last_chorus_id');
            this.refreshDisplay();
            this.showNotification('Setlist cleared', 'info');
        }
    }

    // ---------- launch / view ----------
    // Open one item in its proper UI (chorus iframe or Bible overlay).
    // Updates runnerIndex so subsequent advance/retreat calls walk
    // forward/backward through the mixed-kind setlist.
    openItem(item) {
        if (!item) return;
        const idx = this.setlist.findIndex(i => i === item || SetlistManager.keyOf(i) === SetlistManager.keyOf(item));
        if (idx >= 0) this.runnerIndex = idx;

        if (item.kind === 'verse') {
            // Close any open chorus iframe so the two surfaces don't stack.
            if (window.chorusOverlay && typeof window.chorusOverlay.close === 'function') {
                window.chorusOverlay.close();
            }
            if (window.bibleOverlay && typeof window.bibleOverlay.open === 'function') {
                window.bibleOverlay.open({
                    bookId: item.bookId,
                    chapter: item.chapter,
                    verse: item.verse,
                    setlistContext: { hasItems: this.setlist.length > 1 },
                });
            }
            return;
        }
        // Chorus: close the Bible overlay if it was up, then load the
        // chorus iframe. Feed chorus-display the chorus-only items via
        // the legacy `chorusList` key (so its internal id-based lookup
        // works), but we'll override its prev/next via the parent-side
        // advance() so cross-kind navigation still flows correctly.
        if (window.bibleOverlay && typeof window.bibleOverlay.close === 'function') {
            window.bibleOverlay.close();
        }
        const chorusItems = this.setlist.filter(i => i.kind === 'chorus');
        sessionStorage.setItem('chorusList', JSON.stringify(chorusItems));
        sessionStorage.setItem('currentChorusId', item.id);
        if (window.chorusOverlay && typeof window.chorusOverlay.openChorus === 'function') {
            window.chorusOverlay.openChorus(item.id);
        } else {
            window.location.href = `/Home/ChorusDisplay/${item.id}`;
        }
    }

    // Move forward/backward through the full mixed setlist. Wraps at
    // the ends so the user can keep clicking next without dead-ends.
    // direction: +1 (next) or -1 (prev).
    advance(direction) {
        if (!this.setlist.length) return;
        const dir = direction >= 0 ? 1 : -1;
        // If we don't have a current position (e.g. user is mid-browsing),
        // assume we're "before the first" so +1 lands on item 0.
        const start = this.runnerIndex >= 0 ? this.runnerIndex : (dir > 0 ? -1 : this.setlist.length);
        let nextIdx = start + dir;
        if (nextIdx < 0) nextIdx = this.setlist.length - 1;
        if (nextIdx >= this.setlist.length) nextIdx = 0;
        this.openItem(this.setlist[nextIdx]);
    }
    isRunnerActive() { return this.runnerIndex >= 0 && this.setlist.length > 1; }

    launchSetlist() {
        if (this.setlist.length === 0) {
            this.showNotification('Setlist is empty', 'warning');
            return;
        }
        // If we have a bookmarked chorus and it's still in the list, resume there.
        // Otherwise start at the very first item (chorus or verse).
        const lastId = localStorage.getItem('chap2_setlist_last_chorus_id');
        const resumeIndex = lastId
            ? this.setlist.findIndex(i => i.kind === 'chorus' && i.id === lastId)
            : -1;
        const startItem = resumeIndex >= 0 ? this.setlist[resumeIndex] : this.setlist[0];
        const resumed = resumeIndex > 0;
        this.openItem(startItem);
        const label = startItem.kind === 'verse' ? startItem.ref : startItem.name;
        this.showNotification(resumed ? `Resuming at "${label}"...` : 'Launching setlist...', 'success');
    }

    // ---------- rendering ----------
    refreshDisplay() {
        const setlistEmpty = document.getElementById('setlistEmpty');
        const setlistItems = document.getElementById('setlistItems');
        const setlistCount = document.getElementById('setlistCount');
        const launchBtn = document.getElementById('launchSetlistBtn');
        const saveBtn = document.getElementById('saveSetlistBtn');
        const clearBtn = document.getElementById('clearSetlistBtn');

        if (setlistCount) {
            setlistCount.textContent = `(${this.setlist.length} item${this.setlist.length !== 1 ? 's' : ''})`;
        }
        if (launchBtn) launchBtn.style.display = this.setlist.length > 0 ? 'inline-block' : 'none';
        if (saveBtn)   saveBtn.style.display   = this.setlist.length > 0 ? 'inline-block' : 'none';
        if (clearBtn)  clearBtn.style.display  = this.setlist.length > 0 ? 'inline-block' : 'none';

        if (this.setlist.length === 0) {
            if (setlistEmpty) setlistEmpty.style.display = 'block';
            if (setlistItems) setlistItems.style.display = 'none';
            return;
        }
        if (setlistEmpty) setlistEmpty.style.display = 'none';
        if (setlistItems) setlistItems.style.display = 'block';

        const esc = window.escapeHtml || utils.escapeHtml;
        const html = this.setlist.map((item, index) => this._renderItem(item, index, esc)).join('');
        if (setlistItems) setlistItems.innerHTML = html;

        this.initializeDragAndDrop();
    }

    _renderItem(item, index, esc) {
        const key = SetlistManager.keyOf(item);
        const isFirst = index === 0;
        const isLast  = index === this.setlist.length - 1;
        const moveUpBtn = `<button class="btn-icon" onclick="window.setlistManager.moveUp('${esc(key)}')" title="Move up" ${isFirst ? 'disabled' : ''}><i class="fas fa-arrow-up"></i></button>`;
        const moveDnBtn = `<button class="btn-icon" onclick="window.setlistManager.moveDown('${esc(key)}')" title="Move down" ${isLast ? 'disabled' : ''}><i class="fas fa-arrow-down"></i></button>`;
        const removeBtn = `<button class="btn-icon btn-danger" onclick="window.setlistManager.removeItem('${esc(key)}')" title="Remove"><i class="fas fa-trash"></i></button>`;

        if (item.kind === 'verse') {
            const viewBtn = `<button class="btn-icon" onclick="window.setlistManager.openItem(window.setlistManager.setlist.find(i => window.SetlistManager.keyOf(i) === '${esc(key)}'))" title="Open"><i class="fas fa-eye"></i></button>`;
            return `
                <div class="setlist-item setlist-item--verse" data-setlist-key="${esc(key)}" draggable="true">
                    <div class="setlist-item-drag"><i class="fas fa-grip-vertical"></i></div>
                    <div class="setlist-item-number">${index + 1}</div>
                    <div class="setlist-item-content">
                        <div class="setlist-item-title">
                            <i class="fas fa-bible setlist-item-kind-icon" aria-hidden="true"></i>
                            ${esc(item.ref)}
                        </div>
                        <div class="setlist-item-meta setlist-item-meta--verse">${esc(item.text)}</div>
                    </div>
                    <div class="setlist-item-actions">
                        ${moveUpBtn}
                        ${moveDnBtn}
                        ${viewBtn}
                        ${removeBtn}
                    </div>
                </div>`;
        }

        // Chorus item — preserves existing markup (with kind-aware key + open button).
        const viewBtn = `<button class="btn-icon" onclick="window.setlistManager.openItem(window.setlistManager.setlist.find(i => window.SetlistManager.keyOf(i) === '${esc(key)}'))" title="View"><i class="fas fa-eye"></i></button>`;
        return `
            <div class="setlist-item" data-setlist-key="${esc(key)}" draggable="true">
                <div class="setlist-item-drag"><i class="fas fa-grip-vertical"></i></div>
                <div class="setlist-item-number">${index + 1}</div>
                <div class="setlist-item-content">
                    <div class="setlist-item-title">${esc(item.name || '')}</div>
                    <div class="setlist-item-meta">
                        ${item.key ? `<span><i class="fas fa-music"></i> ${esc(String(item.key))}</span>` : ''}
                        ${item.type ? `<span><i class="fas fa-tag"></i> ${esc(String(item.type))}</span>` : ''}
                        ${item.timeSignature ? `<span><i class="fas fa-clock"></i> ${esc(String(item.timeSignature))}</span>` : ''}
                    </div>
                </div>
                <div class="setlist-item-actions">
                    ${moveUpBtn}
                    ${moveDnBtn}
                    ${viewBtn}
                    ${removeBtn}
                </div>
            </div>`;
    }

    // ---------- drag & drop (kind-agnostic, key-based) ----------
    initializeDragAndDrop() {
        const items = document.querySelectorAll('.setlist-item');
        let draggedItem = null;

        items.forEach(item => {
            item.addEventListener('dragstart', (e) => {
                draggedItem = item;
                item.classList.add('dragging');
                e.dataTransfer.effectAllowed = 'move';
            });
            item.addEventListener('dragend', () => {
                item.classList.remove('dragging');
                draggedItem = null;
            });
            item.addEventListener('dragover', (e) => {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';
                if (!draggedItem || draggedItem === item) return;
                const bounding = item.getBoundingClientRect();
                const offset = bounding.y + bounding.height / 2;
                if (e.clientY - offset > 0) {
                    item.style.borderBottom = '2px solid #2196F3';
                    item.style.borderTop = '';
                } else {
                    item.style.borderTop = '2px solid #2196F3';
                    item.style.borderBottom = '';
                }
            });
            item.addEventListener('dragleave', () => {
                item.style.borderTop = '';
                item.style.borderBottom = '';
            });
            item.addEventListener('drop', (e) => {
                e.preventDefault();
                item.style.borderTop = '';
                item.style.borderBottom = '';
                if (!draggedItem || draggedItem === item) return;
                const draggedKey = draggedItem.getAttribute('data-setlist-key');
                const targetKey = item.getAttribute('data-setlist-key');
                const draggedIndex = this.setlist.findIndex(i => SetlistManager.keyOf(i) === draggedKey);
                const targetIndex = this.setlist.findIndex(i => SetlistManager.keyOf(i) === targetKey);
                if (draggedIndex !== -1 && targetIndex !== -1) {
                    const [moved] = this.setlist.splice(draggedIndex, 1);
                    this.setlist.splice(targetIndex, 0, moved);
                    this.saveToLocalStorage();
                    this.refreshDisplay();
                }
            });
        });
    }

    // ---------- panel buttons ----------
    initializeEventListeners() {
        const launchBtn = document.getElementById('launchSetlistBtn');
        if (launchBtn) launchBtn.addEventListener('click', () => this.launchSetlist());

        const saveBtn = document.getElementById('saveSetlistBtn');
        if (saveBtn) saveBtn.addEventListener('click', () => this.saveToServer());

        const loadBtn = document.getElementById('loadSetlistBtn');
        if (loadBtn) loadBtn.addEventListener('click', () => this.loadFromServer());

        const clearBtn = document.getElementById('clearSetlistBtn');
        if (clearBtn) clearBtn.addEventListener('click', () => this.clearAll());
    }

    // External callers (Bible overlay, search-bar verse rows) dispatch
    // a custom event with the verse payload; we add it without coupling.
    listenForVerseAdds() {
        window.addEventListener('chap2:add-verse-to-setlist', (e) => {
            if (e && e.detail) this.addVerse(e.detail);
        });
    }

    // ---------- server save / load (per-user, named) ----------
    // Save the in-memory setlist to the server under a name. Same name
    // upserts (server replaces all items atomically). Verses + choruses
    // are both first-class items.
    async saveToServer() {
        if (this.setlist.length === 0) {
            this.showNotification('Setlist is empty', 'warning');
            return;
        }
        const name = (prompt('Save setlist as:', this._currentSetlistName || '') || '').trim();
        if (!name) return;

        const payload = {
            name,
            items: this.setlist.map(SetlistManager.toServerPayload).filter(Boolean),
        };

        try {
            const response = await fetch('/Setlists/Save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload),
            });
            if (!response.ok) {
                this.showNotification(`Save failed (${response.status})`, 'error');
                return;
            }
            const saved = await response.json();
            this._currentSetlistName = saved.name || name;
            this.showNotification(`Saved "${this._currentSetlistName}"`, 'success');
        } catch (e) {
            console.error('saveToServer failed', e);
            this.showNotification('Save failed', 'error');
        }
    }

    async loadFromServer() {
        try {
            const response = await fetch('/Setlists/Mine', {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin',
            });
            if (!response.ok) {
                this.showNotification(`Load failed (${response.status})`, 'error');
                return;
            }
            const summaries = await response.json();
            if (!Array.isArray(summaries) || summaries.length === 0) {
                this.showNotification('No saved setlists yet', 'info');
                return;
            }
            this._showLoadModal(summaries);
        } catch (e) {
            console.error('loadFromServer failed', e);
            this.showNotification('Load failed', 'error');
        }
    }

    async _loadById(id) {
        try {
            const response = await fetch(`/Setlists/Detail/${encodeURIComponent(id)}`, {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin',
            });
            if (!response.ok) {
                this.showNotification(`Load failed (${response.status})`, 'error');
                return;
            }
            const setlist = await response.json();
            const imported = (setlist.items || []).map(SetlistManager.fromServerItem).filter(Boolean);
            this.setlist = imported;
            this._currentSetlistName = setlist.name || '';
            this.runnerIndex = -1;
            this.saveToLocalStorage();
            this.refreshDisplay();
            this.showNotification(`Loaded "${this._currentSetlistName}" (${imported.length} items)`, 'success');
        } catch (e) {
            console.error('_loadById failed', e);
            this.showNotification('Load failed', 'error');
        }
    }

    async _deleteById(id, name) {
        if (!confirm(`Delete saved setlist "${name}"? This cannot be undone.`)) return false;
        try {
            const response = await fetch(`/Setlists/Delete/${encodeURIComponent(id)}`, {
                method: 'DELETE',
                credentials: 'same-origin',
            });
            if (!response.ok) {
                this.showNotification(`Delete failed (${response.status})`, 'error');
                return false;
            }
            this.showNotification(`Deleted "${name}"`, 'info');
            return true;
        } catch (e) {
            console.error('_deleteById failed', e);
            this.showNotification('Delete failed', 'error');
            return false;
        }
    }

    _showLoadModal(summaries) {
        // Tear down any previous modal so we don't stack overlays.
        const previous = document.getElementById('setlistLoadModal');
        if (previous) previous.remove();

        const overlay = document.createElement('div');
        overlay.id = 'setlistLoadModal';
        overlay.style.cssText = `
            position: fixed; inset: 0; background: rgba(0,0,0,0.6);
            display: flex; align-items: center; justify-content: center;
            z-index: 10001;`;

        const card = document.createElement('div');
        card.style.cssText = `
            width: min(480px, 92vw); max-height: 80vh; overflow: auto;
            background: #1f1c18; color: #f0ece2;
            border: 1px solid rgba(255,255,255,0.08); border-radius: 12px;
            padding: 20px; display: flex; flex-direction: column; gap: 12px;`;

        const title = document.createElement('h3');
        title.textContent = 'Load saved setlist';
        title.style.cssText = 'margin: 0 0 8px; font-size: 1.05rem;';
        card.appendChild(title);

        const list = document.createElement('div');
        list.style.cssText = 'display: flex; flex-direction: column; gap: 8px;';

        const esc = window.escapeHtml || (s => String(s));
        const sorted = [...summaries].sort((a, b) =>
            (b.updatedAt || b.createdAt || '').localeCompare(a.updatedAt || a.createdAt || ''));

        sorted.forEach(s => {
            const row = document.createElement('div');
            row.style.cssText = `
                display: flex; align-items: center; gap: 10px;
                padding: 10px 12px; background: rgba(255,255,255,0.04);
                border: 1px solid rgba(255,255,255,0.08); border-radius: 8px;`;

            const meta = document.createElement('div');
            meta.style.cssText = 'flex: 1; min-width: 0;';
            meta.innerHTML = `
                <div style="font-weight:600;">${esc(s.name)}</div>
                <div style="font-size:0.8rem; color: rgba(245,241,234,0.6);">${s.itemCount} item${s.itemCount === 1 ? '' : 's'}</div>`;
            row.appendChild(meta);

            const loadBtn = document.createElement('button');
            loadBtn.textContent = 'Load';
            loadBtn.style.cssText = `
                padding: 6px 12px; background: var(--primary-color, #c9a24a);
                color: #1f1c18; border: none; border-radius: 6px; cursor: pointer; font-weight: 600;`;
            loadBtn.addEventListener('click', async () => {
                overlay.remove();
                await this._loadById(s.id);
            });
            row.appendChild(loadBtn);

            const deleteBtn = document.createElement('button');
            deleteBtn.textContent = 'Delete';
            deleteBtn.style.cssText = `
                padding: 6px 12px; background: rgba(168,52,42,0.5);
                color: #f6c9c5; border: none; border-radius: 6px; cursor: pointer;`;
            deleteBtn.addEventListener('click', async () => {
                const ok = await this._deleteById(s.id, s.name);
                if (ok) row.remove();
            });
            row.appendChild(deleteBtn);

            list.appendChild(row);
        });
        card.appendChild(list);

        const cancel = document.createElement('button');
        cancel.textContent = 'Cancel';
        cancel.style.cssText = `
            margin-top: 8px; padding: 8px 14px; background: rgba(255,255,255,0.08);
            color: #f0ece2; border: 1px solid rgba(255,255,255,0.12);
            border-radius: 6px; cursor: pointer; align-self: flex-end;`;
        cancel.addEventListener('click', () => overlay.remove());
        card.appendChild(cancel);

        overlay.appendChild(card);
        overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });
        document.body.appendChild(overlay);
    }

    // Convert in-memory item -> server payload (drops chorus metadata; server hydrates).
    static toServerPayload(item) {
        if (!item) return null;
        if (item.kind === 'verse') {
            return {
                kind: 'verse',
                bookId: item.bookId,
                bookName: item.bookName,
                chapter: item.chapter,
                verse: item.verse,
                text: item.text,
                ref: item.ref,
            };
        }
        if (!item.id) return null;
        return { kind: 'chorus', chorusId: item.id };
    }

    // Convert server item -> in-memory item (uses server-hydrated chorus metadata when present).
    static fromServerItem(it) {
        if (!it) return null;
        if (it.kind === 'verse') {
            return SetlistManager.normalizeItem({
                kind: 'verse',
                bookId: it.bookId,
                bookName: it.bookName,
                chapter: it.chapter,
                verse: it.verse,
                text: it.text,
                ref: it.ref,
            });
        }
        // chorus
        if (!it.chorusId) return null;
        return SetlistManager.normalizeItem({
            kind: 'chorus',
            id: it.chorusId,
            name: it.chorusName,
            key: it.chorusKey,
            type: it.chorusType,
            timeSignature: it.chorusTimeSignature,
        });
    }

    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `setlist-notification setlist-notification-${type}`;
        const icon = document.createElement('i');
        icon.className = `fas fa-${type === 'success' ? 'check-circle' : type === 'warning' ? 'exclamation-triangle' : type === 'error' ? 'times-circle' : 'info-circle'}`;
        const span = document.createElement('span');
        span.textContent = message;
        notification.appendChild(icon);
        notification.appendChild(span);
        notification.style.cssText = `
            position: fixed; top: 20px; right: 20px;
            background: ${type === 'success' ? '#4CAF50' : type === 'warning' ? '#FF9800' : type === 'error' ? '#f44336' : '#2196F3'};
            color: white; padding: 15px 20px; border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            z-index: 10000;
            display: flex; align-items: center; gap: 10px;
            animation: slideIn 0.3s ease-out;`;
        document.body.appendChild(notification);
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => {
                if (notification.parentNode) document.body.removeChild(notification);
            }, 300);
        }, 3000);
    }
}

window.SetlistManager = SetlistManager;

window.refreshSetlistDisplay = function () {
    if (window.setlistManager) window.setlistManager.refreshDisplay();
};

window.addToSetlist = function (chorusId) {
    const chorus = window.currentChorusList?.find(c => c.id === chorusId);
    if (!chorus) {
        console.error('Chorus not found in current results');
        return;
    }
    if (window.setlistManager) window.setlistManager.addChorus(chorus);
    else console.error('Setlist manager not initialized');
};

document.addEventListener('DOMContentLoaded', function () {
    window.setlistManager = new SetlistManager();
    window.setlistManager.refreshDisplay();
    debug('Setlist manager initialized');
});

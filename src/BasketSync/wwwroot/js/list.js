// ── Auth check ──
if (!localStorage.getItem("token")) {
    window.location.href = "login.html";
}

function authHeaders(extra = {}) {
    return { "Authorization": "Bearer " + localStorage.getItem("token"), ...extra };
}

function authFetch(url, options = {}) {
    options.headers = authHeaders(options.headers || {});
    return fetch(url, options).then(res => {
        if (res.status === 401) {
            localStorage.clear();
            window.location.href = "login.html";
        }
        return res;
    });
}

// ── State ──
const listId = new URLSearchParams(location.search).get("id");
const apiBase = `/api/lists/${listId}`;
let currentItems = [];
let contextTarget = null; // {id, listItemId} for context menu

// ── DOM refs ──
const titleElem    = document.getElementById("list-title");
const container    = document.getElementById("items-container");
const counterElem  = document.getElementById("counter");
const loadingElem  = document.getElementById("loading");
const fabBtn       = document.getElementById("fab-add");
const toastElem    = document.getElementById("toast");

// ── Init ──
document.addEventListener("DOMContentLoaded", () => {
    loadList();
    setupModals();
    setupContextMenu();
    setupFab();
    setupInlineQty();
});

// ── Toast ──
function showToast(msg, isError = false) {
    toastElem.textContent = msg;
    toastElem.className = "toast toast--visible" + (isError ? " toast--error" : "");
    setTimeout(() => { toastElem.className = "toast"; }, 2500);
}

// ── Data loading ──
async function loadList() {
    try {
        const res = await authFetch(apiBase);
        if (!res.ok) throw new Error("Ошибка загрузки списка");
        const data = await res.json();

        document.title = data.name + " — BasketSync";
        titleElem.textContent = data.name;
        currentItems = data.items || [];
        renderItems(currentItems);
    } catch (e) {
        container.innerHTML = '<div class="empty-state">Не удалось загрузить список</div>';
        showToast(e.message, true);
    }
}

// ── Render ──
function renderItems(items) {
    container.innerHTML = "";

    if (items.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-state__icon">🛒</div>
                <p>Список пуст</p>
                <p class="empty-state__hint">Нажмите + чтобы добавить товар</p>
            </div>`;
        updateCounter(0, 0);
        return;
    }

    const unchecked = items.filter(i => !i.isChecked);
    const checked   = items.filter(i => i.isChecked);

    // Group unchecked by category
    const groups = {};
    for (const item of unchecked) {
        const cat = item.categoryName || "Без категории";
        if (!groups[cat]) groups[cat] = [];
        groups[cat].push(item);
    }

    // Render unchecked groups
    for (const [category, catItems] of Object.entries(groups).sort((a, b) => a[0].localeCompare(b[0]))) {
        const section = document.createElement("section");
        section.className = "category-group";
        section.innerHTML = `<h2 class="category-title">${category}</h2>`;

        for (const item of catItems) {
            section.appendChild(createItemCard(item, false));
        }
        container.appendChild(section);
    }

    // Render checked section
    if (checked.length > 0) {
        const section = document.createElement("section");
        section.className = "category-group category-group--checked";
        section.innerHTML = `<h2 class="category-title category-title--checked">Куплено</h2>`;

        for (const item of checked) {
            section.appendChild(createItemCard(item, true));
        }
        container.appendChild(section);
    }

    updateCounter(checked.length, items.length);
}

function createItemCard(item, isChecked) {
    const card = document.createElement("div");
    card.className = "item-card" + (isChecked ? " item-card--checked" : "");
    card.dataset.id = item.id;

    const commentHtml = item.comment
        ? `<span class="item-comment">${escapeHtml(item.comment)}</span>`
        : "";

    card.innerHTML = `
        <label class="item-check">
            <input type="checkbox" ${isChecked ? "checked" : ""} data-id="${item.id}">
            <span class="checkmark"></span>
        </label>
        <div class="item-info">
            <span class="item-name">${item.itemName || "—"}</span>
            ${commentHtml}
        </div>
        <div class="qty-inline">
            <button class="qty-inline-btn" data-id="${item.id}" data-delta="-1">−</button>
            <input type="number" class="qty-inline-input" data-id="${item.id}" value="${item.quantity}" min="1">
            <button class="qty-inline-btn" data-id="${item.id}" data-delta="1">+</button>
            <span class="qty-inline-unit">${item.unitName || ""}</span>
        </div>
        <button class="item-menu-btn" data-id="${item.id}" aria-label="Меню">⋮</button>
    `;
    return card;
}

function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}

function updateCounter(bought, total) {
    if (total === 0) {
        counterElem.textContent = "";
        return;
    }
    counterElem.textContent = `${bought} из ${total}`;
}

// ── Toggle checkbox ──
container.addEventListener("change", async (e) => {
    if (!e.target.matches("input[type='checkbox']")) return;
    const id = +e.target.dataset.id;
    try {
        await authFetch(`/api/items/${id}/toggle`, { method: "PATCH" });
        await loadList();
    } catch {
        showToast("Ошибка при обновлении", true);
    }
});

// ── Context menu ──
function setupContextMenu() {
    const menu = document.getElementById("context-menu");

    container.addEventListener("click", (e) => {
        const btn = e.target.closest(".item-menu-btn");
        if (!btn) return;
        e.stopPropagation();

        contextTarget = +btn.dataset.id;
        const rect = btn.getBoundingClientRect();
        menu.style.top = rect.bottom + 4 + "px";
        menu.style.right = (window.innerWidth - rect.right) + "px";
        menu.style.left = "auto";
        menu.classList.add("context-menu--visible");
    });

    document.addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
    });

    document.getElementById("ctx-edit").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        const item = currentItems.find(i => i.id === contextTarget);
        if (item) openEditItemModal(item);
    });

    document.getElementById("ctx-delete").addEventListener("click", async () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        try {
            const res = await authFetch(`${apiBase}/items`, {
                method: "DELETE",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ itemId: contextTarget })
            });
            if (!res.ok) throw new Error();
            showToast("Товар удалён");
            await loadList();
        } catch {
            showToast("Ошибка удаления", true);
        }
    });
}

// ── Inline quantity controls ──
function setupInlineQty() {
    // +/- buttons
    container.addEventListener("click", async (e) => {
        const btn = e.target.closest(".qty-inline-btn");
        if (!btn) return;

        const itemId = +btn.dataset.id;
        const delta = +btn.dataset.delta;
        const item = currentItems.find(i => i.id === itemId);
        if (!item) return;

        const newQty = item.quantity + delta;
        if (newQty < 1) return;

        try {
            const res = await authFetch(`${apiBase}/items/${itemId}`, {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ quantity: newQty })
            });
            if (!res.ok) throw new Error();
            item.quantity = newQty;
            btn.closest(".item-card").querySelector(".qty-inline-input").value = newQty;
        } catch {
            showToast("Ошибка сохранения", true);
        }
    });

    // Direct input change
    container.addEventListener("change", async (e) => {
        if (!e.target.matches(".qty-inline-input")) return;

        const input = e.target;
        const itemId = +input.dataset.id;
        const item = currentItems.find(i => i.id === itemId);
        if (!item) return;

        let newQty = parseInt(input.value) || 1;
        if (newQty < 1) newQty = 1;
        input.value = newQty;

        if (newQty === item.quantity) return;

        try {
            const res = await authFetch(`${apiBase}/items/${itemId}`, {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ quantity: newQty })
            });
            if (!res.ok) throw new Error();
            item.quantity = newQty;
        } catch {
            input.value = item.quantity;
            showToast("Ошибка сохранения", true);
        }
    });
}

// ── FAB + Add item modal ──
function setupFab() {
    fabBtn.addEventListener("click", () => {
        document.getElementById("item-search").value = "";
        document.getElementById("search-results").innerHTML = "";
        openModal("add-item-modal");
        setTimeout(() => document.getElementById("item-search").focus(), 100);
    });

    // Search input
    let searchTimer;
    document.getElementById("item-search").addEventListener("input", (e) => {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => searchItems(e.target.value.trim()), 300);
    });

    // Create new item button
    document.getElementById("btn-create-new-item").addEventListener("click", () => {
        closeModal("add-item-modal");
        openCreateItemModal();
    });
}

async function searchItems(query) {
    const resultsDiv = document.getElementById("search-results");
    if (!query) {
        resultsDiv.innerHTML = "";
        return;
    }

    try {
        const res = await authFetch(`/api/items?search=${encodeURIComponent(query)}`);
        let items = await res.json();

        // Filter out items already in the list
        const existingItemIds = new Set(currentItems.map(i => i.itemId));
        items = items.filter(item => !existingItemIds.has(item.id));

        if (items.length === 0) {
            resultsDiv.innerHTML = '<div class="search-empty">Ничего не найдено</div>';
            return;
        }

        resultsDiv.innerHTML = items.map(item => `
            <div class="search-result-item" data-item-id="${item.id}">
                <div class="search-result-info">
                    <span class="search-result-name">${item.name}</span>
                    <span class="search-result-meta">${item.categoryName} · ${item.unitName}</span>
                </div>
                <button class="btn-add-to-list" data-item-id="${item.id}">+</button>
            </div>
        `).join("");
    } catch {
        resultsDiv.innerHTML = '<div class="search-empty">Ошибка поиска</div>';
    }
}

// Click handler for adding item from search results
document.getElementById("search-results").addEventListener("click", async (e) => {
    const btn = e.target.closest(".btn-add-to-list");
    if (!btn) return;

    const itemId = +btn.dataset.itemId;
    try {
        await authFetch(`${apiBase}/items`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ itemId, quantity: 1 })
        });
        closeModal("add-item-modal");
        showToast("Товар добавлен");
        await loadList();
    } catch {
        showToast("Ошибка добавления", true);
    }
});

// ── Create item modal ──
async function openCreateItemModal() {
    // Load categories and units
    try {
        const [catRes, unitRes] = await Promise.all([
            authFetch("/api/categories"),
            authFetch("/api/units")
        ]);
        const categories = await catRes.json();
        const units = await unitRes.json();

        const catSelect = document.getElementById("new-item-category");
        catSelect.innerHTML = categories.map(c =>
            `<option value="${c.id}">${c.name}</option>`
        ).join("");

        const unitSelect = document.getElementById("new-item-unit");
        unitSelect.innerHTML = units.map(u =>
            `<option value="${u.id}">${u.name}</option>`
        ).join("");
    } catch {
        showToast("Ошибка загрузки справочников", true);
        return;
    }

    document.getElementById("new-item-name").value = "";
    openModal("create-item-modal");
    setTimeout(() => document.getElementById("new-item-name").focus(), 100);
}

document.getElementById("btn-save-new-item").addEventListener("click", async () => {
    const name = document.getElementById("new-item-name").value.trim();
    const categoryId = +document.getElementById("new-item-category").value;
    const unitId = +document.getElementById("new-item-unit").value;

    if (!name) {
        showToast("Введите название товара", true);
        return;
    }

    try {
        const res = await authFetch("/api/items", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name, categoryId, unitId })
        });

        if (res.status === 409) {
            showToast("Такой товар уже существует", true);
            return;
        }
        if (!res.ok) throw new Error();

        const newItem = await res.json();
        closeModal("create-item-modal");

        // Immediately add to current list
        await authFetch(`${apiBase}/items`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ itemId: newItem.id, quantity: 1 })
        });

        showToast("Товар создан и добавлен");
        await loadList();
    } catch {
        showToast("Ошибка создания товара", true);
    }
});

// ── Modal helpers ──
function openModal(id) {
    document.getElementById(id).classList.add("modal--visible");
    document.body.classList.add("no-scroll");
}

function closeModal(id) {
    document.getElementById(id).classList.remove("modal--visible");
    document.body.classList.remove("no-scroll");
}

function setupModals() {
    // Close on overlay click
    document.querySelectorAll(".modal-overlay").forEach(overlay => {
        overlay.addEventListener("click", (e) => {
            if (e.target === overlay) closeModal(overlay.id);
        });
    });

    // Close buttons
    document.querySelectorAll("[data-close]").forEach(btn => {
        btn.addEventListener("click", () => closeModal(btn.dataset.close));
    });

    // Escape key
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            document.querySelectorAll(".modal--visible").forEach(m => closeModal(m.id));
        }
    });

    // Save edit button
    document.getElementById("btn-save-edit").addEventListener("click", saveEditItem);
}

// ── Edit item modal ──
let editingItemId = null;

async function openEditItemModal(item) {
    editingItemId = item.id;
    document.getElementById("edit-item-title").textContent = item.itemName || "Редактировать";
    document.getElementById("edit-item-comment").value = item.comment || "";

    try {
        const [catRes, unitRes] = await Promise.all([
            authFetch("/api/categories"),
            authFetch("/api/units")
        ]);
        const categories = await catRes.json();
        const units = await unitRes.json();

        const catSelect = document.getElementById("edit-item-category");
        catSelect.innerHTML = categories.map(c =>
            `<option value="${c.id}" ${c.id === item.categoryId ? "selected" : ""}>${c.name}</option>`
        ).join("");

        const unitSelect = document.getElementById("edit-item-unit");
        unitSelect.innerHTML = units.map(u =>
            `<option value="${u.id}" ${u.id === item.unitId ? "selected" : ""}>${u.name}</option>`
        ).join("");
    } catch {
        showToast("Ошибка загрузки справочников", true);
        return;
    }

    openModal("edit-item-modal");
}

async function saveEditItem() {
    if (!editingItemId) return;

    const comment = document.getElementById("edit-item-comment").value.trim();
    const categoryId = +document.getElementById("edit-item-category").value;
    const unitId = +document.getElementById("edit-item-unit").value;

    try {
        const res = await authFetch(`${apiBase}/items/${editingItemId}`, {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ comment, categoryId, unitId })
        });
        if (!res.ok) throw new Error();
        closeModal("edit-item-modal");
        showToast("Сохранено");
        await loadList();
    } catch {
        showToast("Ошибка сохранения", true);
    }
}

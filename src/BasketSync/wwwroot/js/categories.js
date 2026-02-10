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
let categories = [];
let contextTarget = null;
let editingId = null; // null = create, number = edit

// ── DOM refs ──
const container = document.getElementById("categories-container");
const toastElem = document.getElementById("toast");

// ── Init ──
document.addEventListener("DOMContentLoaded", () => {
    loadCategories();
    setupModals();
    setupContextMenu();
    setupFab();
});

// ── Toast ──
function showToast(msg, isError = false) {
    toastElem.textContent = msg;
    toastElem.className = "toast toast--visible" + (isError ? " toast--error" : "");
    setTimeout(() => { toastElem.className = "toast"; }, 2500);
}

function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
}

// ── Data loading ──
async function loadCategories() {
    try {
        const res = await authFetch("/api/categories");
        if (!res.ok) throw new Error();
        categories = await res.json();
        renderCategories();
    } catch {
        container.innerHTML = '<div class="empty-state">Не удалось загрузить категории</div>';
    }
}

// ── Render ──
function renderCategories() {
    container.innerHTML = "";

    if (categories.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-state__icon">📂</div>
                <p>Нет категорий</p>
                <p class="empty-state__hint">Нажмите + чтобы создать</p>
            </div>`;
        return;
    }

    for (const cat of categories) {
        const card = document.createElement("div");
        card.className = "item-card";
        card.dataset.id = cat.id;

        const commentHtml = cat.comment
            ? `<span class="item-comment">${escapeHtml(cat.comment)}</span>`
            : "";

        card.innerHTML = `
            <div class="item-info">
                <span class="item-name">${escapeHtml(cat.name)}</span>
                ${commentHtml}
            </div>
            <button class="item-menu-btn" data-id="${cat.id}" aria-label="Меню">⋮</button>
        `;
        container.appendChild(card);
    }
}

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
        const cat = categories.find(c => c.id === contextTarget);
        if (cat) openCategoryModal(cat);
    });

    document.getElementById("ctx-delete").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        const cat = categories.find(c => c.id === contextTarget);
        if (!cat) return;
        document.getElementById("confirm-text").textContent =
            `Категория «${cat.name}» будет удалена.`;
        openModal("confirm-modal");
    });

    document.getElementById("btn-confirm-delete").addEventListener("click", async () => {
        closeModal("confirm-modal");
        if (!contextTarget) return;
        try {
            const res = await authFetch(`/api/categories/${contextTarget}`, { method: "DELETE" });
            if (res.status === 409) {
                const data = await res.json().catch(() => null);
                showToast(data?.error || "К категории привязаны товары", true);
                return;
            }
            if (!res.ok) throw new Error();
            showToast("Категория удалена");
            await loadCategories();
        } catch {
            showToast("Ошибка удаления", true);
        }
    });
}

// ── FAB ──
function setupFab() {
    document.getElementById("fab-add").addEventListener("click", () => {
        openCategoryModal(null);
    });
}

// ── Category modal ──
function openCategoryModal(cat) {
    editingId = cat ? cat.id : null;
    document.getElementById("category-modal-title").textContent =
        cat ? "Редактировать категорию" : "Новая категория";
    document.getElementById("category-name").value = cat ? cat.name : "";
    document.getElementById("category-comment").value = cat ? (cat.comment || "") : "";
    openModal("category-modal");
    setTimeout(() => document.getElementById("category-name").focus(), 100);
}

document.getElementById("btn-save-category").addEventListener("click", async () => {
    const name = document.getElementById("category-name").value.trim();
    const comment = document.getElementById("category-comment").value.trim() || null;

    if (!name) {
        showToast("Введите название", true);
        return;
    }

    try {
        let res;
        if (editingId) {
            res = await authFetch(`/api/categories/${editingId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name, comment })
            });
        } else {
            res = await authFetch("/api/categories", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name, comment })
            });
        }

        if (res.status === 409) {
            showToast("Такая категория уже существует", true);
            return;
        }
        if (!res.ok) throw new Error();

        closeModal("category-modal");
        showToast(editingId ? "Категория обновлена" : "Категория создана");
        await loadCategories();
    } catch {
        showToast("Ошибка сохранения", true);
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
    document.querySelectorAll(".modal-overlay").forEach(overlay => {
        overlay.addEventListener("click", (e) => {
            if (e.target === overlay) closeModal(overlay.id);
        });
    });

    document.querySelectorAll("[data-close]").forEach(btn => {
        btn.addEventListener("click", () => closeModal(btn.dataset.close));
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            document.querySelectorAll(".modal--visible").forEach(m => closeModal(m.id));
        }
    });
}

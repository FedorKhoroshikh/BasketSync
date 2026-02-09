// ── State ──
let lists = [];
let contextTarget = null; // list id for context menu
let editingListId = null; // null = create, number = rename

// ── DOM refs ──
const container = document.getElementById("lists-container");
const fabBtn    = document.getElementById("fab-add");
const toastElem = document.getElementById("toast");

// ── Init ──
document.addEventListener("DOMContentLoaded", () => {
    loadShoppingLists();
    setupModals();
    setupContextMenu();
    setupFab();
    setupListModal();
});

// ── Toast ──
function showToast(msg, isError = false) {
    toastElem.textContent = msg;
    toastElem.className = "toast toast--visible" + (isError ? " toast--error" : "");
    setTimeout(() => { toastElem.className = "toast"; }, 2500);
}

// ── Data loading ──
async function loadShoppingLists() {
    try {
        const res = await fetch("/api/users/1/lists");
        if (!res.ok) throw new Error("Ошибка при загрузке списков");

        lists = await res.json();
        renderLists(lists);
    } catch (err) {
        container.innerHTML = '<div class="empty-state">Не удалось загрузить списки</div>';
        showToast(err.message, true);
    }
}

// ── Render ──
function renderLists(items) {
    container.innerHTML = "";

    if (items.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-state__icon">📋</div>
                <p>У вас пока нет списков</p>
                <p class="empty-state__hint">Нажмите + чтобы создать список покупок</p>
            </div>`;
        return;
    }

    for (const list of items) {
        container.appendChild(createListCard(list));
    }
}

function createListCard(list) {
    const card = document.createElement("div");
    card.className = "list-card";
    card.dataset.id = list.id;

    const itemCount = list.items ? list.items.length : 0;
    const checkedCount = list.items ? list.items.filter(i => i.isChecked).length : 0;

    card.innerHTML = `
        <div class="list-card__body" data-id="${list.id}">
            <div class="list-card__info">
                <span class="list-card__name">${list.name}</span>
                ${itemCount > 0
                    ? `<span class="list-card__meta">${checkedCount} из ${itemCount} куплено</span>`
                    : `<span class="list-card__meta">Пусто</span>`}
            </div>
            <svg class="list-card__arrow" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 18l6-6-6-6"/>
            </svg>
        </div>
        <button class="list-card__menu" data-id="${list.id}" aria-label="Меню">⋮</button>
    `;

    // Navigate on card body click
    card.querySelector(".list-card__body").addEventListener("click", () => {
        window.location.href = `list.html?id=${list.id}`;
    });

    return card;
}

// ── FAB ──
function setupFab() {
    fabBtn.addEventListener("click", () => {
        editingListId = null;
        document.getElementById("list-modal-title").textContent = "Новый список";
        document.getElementById("list-name-input").value = "";
        openModal("list-modal");
        setTimeout(() => document.getElementById("list-name-input").focus(), 100);
    });
}

// ── List modal (create / rename) ──
function setupListModal() {
    document.getElementById("btn-save-list").addEventListener("click", async () => {
        const name = document.getElementById("list-name-input").value.trim();
        if (!name) {
            showToast("Введите название списка", true);
            return;
        }

        try {
            if (editingListId) {
                // Rename
                const res = await fetch(`/api/lists/${editingListId}`, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ name })
                });
                if (res.status === 409) {
                    showToast("Список с таким именем уже существует", true);
                    return;
                }
                if (!res.ok) throw new Error();
                showToast("Список переименован");
            } else {
                // Create
                const res = await fetch("/api/lists", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ name, userId: 1 })
                });
                if (res.status === 409) {
                    showToast("Список с таким именем уже существует", true);
                    return;
                }
                if (!res.ok) throw new Error();
                showToast("Список создан");
            }

            closeModal("list-modal");
            await loadShoppingLists();
        } catch {
            showToast("Ошибка сохранения", true);
        }
    });

    // Submit on Enter
    document.getElementById("list-name-input").addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
            e.preventDefault();
            document.getElementById("btn-save-list").click();
        }
    });
}

// ── Context menu ──
function setupContextMenu() {
    const menu = document.getElementById("context-menu");

    container.addEventListener("click", (e) => {
        const btn = e.target.closest(".list-card__menu");
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

    // Rename
    document.getElementById("ctx-rename").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        const list = lists.find(l => l.id === contextTarget);
        if (!list) return;

        editingListId = contextTarget;
        document.getElementById("list-modal-title").textContent = "Переименовать";
        document.getElementById("list-name-input").value = list.name;
        openModal("list-modal");
        setTimeout(() => {
            const input = document.getElementById("list-name-input");
            input.focus();
            input.select();
        }, 100);
    });

    // Delete
    document.getElementById("ctx-delete").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        const list = lists.find(l => l.id === contextTarget);
        if (!list) return;

        document.getElementById("confirm-text").textContent =
            `Список "${list.name}" и все его товары будут удалены.`;
        openModal("confirm-modal");
    });

    // Confirm delete
    document.getElementById("btn-confirm-delete").addEventListener("click", async () => {
        if (!contextTarget) return;
        try {
            const res = await fetch(`/api/lists/${contextTarget}`, { method: "DELETE" });
            if (!res.ok) throw new Error();
            closeModal("confirm-modal");
            showToast("Список удалён");
            await loadShoppingLists();
        } catch {
            showToast("Ошибка удаления", true);
        }
    });
}

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

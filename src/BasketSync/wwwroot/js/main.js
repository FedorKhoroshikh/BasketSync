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

function currentUserId() {
    return parseInt(localStorage.getItem("userId") || "0");
}

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
    displayUserName();
    loadShoppingLists();
    setupModals();
    setupContextMenu();
    setupFab();
    setupListModal();
    setupLogout();
    setupVisibilityRefresh();
});

// ── User display + Logout ──
function displayUserName() {
    const nameElem = document.getElementById("user-name");
    if (nameElem) {
        nameElem.textContent = localStorage.getItem("userName") || "";
    }
}

function setupLogout() {
    const logoutBtn = document.getElementById("btn-logout");
    if (logoutBtn) {
        logoutBtn.addEventListener("click", () => {
            localStorage.clear();
            window.location.href = "login.html";
        });
    }
}

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
async function loadShoppingLists() {
    try {
        const res = await authFetch("/api/users/me/lists");
        if (!res.ok) throw new Error("Ошибка при загрузке списков");

        lists = await res.json();
        renderLists(lists);
    } catch (err) {
        container.innerHTML = '<div class="empty-state">Не удалось загрузить списки</div>';
        showToast(err.message, true);
    }
}

// ── Visibility refresh ──
function setupVisibilityRefresh() {
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) loadShoppingLists();
    });
}

// ── Render ──
function renderLists(items) {
    container.innerHTML = "";

    if (items.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-state__icon">&#x1F4CB;</div>
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
    const isOwn = list.userId === currentUserId();

    const sharedBadge = list.isShared && !isOwn
        ? '<span class="list-card__badge">&#x1F465;</span>'
        : '';

    card.innerHTML = `
        <div class="list-card__body" data-id="${list.id}">
            <div class="list-card__info">
                <span class="list-card__name">${escapeHtml(list.name)} ${sharedBadge}</span>
                ${itemCount > 0
                    ? `<span class="list-card__meta">${checkedCount} из ${itemCount} куплено</span>`
                    : `<span class="list-card__meta">Пусто</span>`}
            </div>
            <svg class="list-card__arrow" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 18l6-6-6-6"/>
            </svg>
        </div>
        ${isOwn ? `<button class="list-card__menu" data-id="${list.id}" aria-label="Меню">&#x22EE;</button>` : ''}
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
        document.getElementById("list-shared").checked = true;
        openModal("list-modal");
        setTimeout(() => document.getElementById("list-name-input").focus(), 100);
    });
}

// ── List modal (create / rename) ──
function setupListModal() {
    document.getElementById("btn-save-list").addEventListener("click", async () => {
        const name = document.getElementById("list-name-input").value.trim();
        const isShared = document.getElementById("list-shared").checked;
        if (!name) {
            showToast("Введите название списка", true);
            return;
        }

        try {
            if (editingListId) {
                // Rename
                const res = await authFetch(`/api/lists/${editingListId}`, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ name, isShared })
                });
                if (res.status === 409) {
                    showToast("Список с таким именем уже существует", true);
                    return;
                }
                if (!res.ok) throw new Error();
                showToast("Список обновлён");
            } else {
                // Create
                const res = await authFetch("/api/lists", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ name, isShared })
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
        document.getElementById("list-shared").checked = !!list.isShared;
        openModal("list-modal");
        setTimeout(() => {
            const input = document.getElementById("list-name-input");
            input.focus();
            input.select();
        }, 100);
    });

    // Share
    document.getElementById("ctx-share").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        openShareModal(contextTarget);
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
            const res = await authFetch(`/api/lists/${contextTarget}`, { method: "DELETE" });
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

// ── Share modal ──
let shareListId = null;

async function openShareModal(listId) {
    shareListId = listId;
    const list = lists.find(l => l.id === listId);
    if (!list) return;

    const shareAllCheckbox = document.getElementById("share-all");
    shareAllCheckbox.checked = !!list.isShared;

    const usersContainer = document.getElementById("share-users-list");
    usersContainer.innerHTML = '<div class="loading">Загрузка...</div>';
    openModal("share-modal");

    try {
        const [usersRes, sharesRes] = await Promise.all([
            authFetch("/api/users"),
            authFetch(`/api/lists/${listId}/shares`)
        ]);

        const allUsers = await usersRes.json();
        const sharedUserIds = await sharesRes.json();
        const myId = currentUserId();

        const otherUsers = allUsers.filter(u => u.id !== myId);
        if (otherUsers.length === 0) {
            usersContainer.innerHTML = '<p class="empty-state__hint">Нет других пользователей</p>';
            return;
        }

        usersContainer.innerHTML = otherUsers.map(u => `
            <label class="checkbox-label">
                <input type="checkbox" value="${u.id}" ${sharedUserIds.includes(u.id) ? "checked" : ""}>
                ${escapeHtml(u.name)}
            </label>
        `).join("");

        // "Select all" toggles user checkboxes
        shareAllCheckbox.addEventListener("change", () => {
            usersContainer.querySelectorAll("input[type=checkbox]").forEach(cb => {
                cb.checked = shareAllCheckbox.checked;
            });
        });
    } catch {
        usersContainer.innerHTML = '<p class="empty-state__hint">Ошибка загрузки</p>';
    }
}

document.getElementById("btn-save-shares").addEventListener("click", async () => {
    if (!shareListId) return;
    const isShared = document.getElementById("share-all").checked;
    const userIds = [...document.querySelectorAll("#share-users-list input[type=checkbox]:checked")]
        .map(cb => +cb.value);

    try {
        // Update isShared flag via rename (keeping current name)
        const list = lists.find(l => l.id === shareListId);
        if (list) {
            await authFetch(`/api/lists/${shareListId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name: list.name, isShared })
            });
        }

        // Update specific user shares
        await authFetch(`/api/lists/${shareListId}/shares`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ userIds })
        });

        closeModal("share-modal");
        showToast("Доступ обновлён");
        await loadShoppingLists();
    } catch {
        showToast("Ошибка сохранения", true);
    }
});

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

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
let cards = [];
let contextTarget = null;
let editingId = null;
let detailCardId = null;
let selectedImageFile = null;
let searchFilter = "";

const IDENTIFIER_TYPES = ["Номер", "QR", "Штрих-код", "Изображение"];

// ── DOM refs ──
const container = document.getElementById("cards-container");
const toastElem = document.getElementById("toast");

// ── Init ──
document.addEventListener("DOMContentLoaded", () => {
    loadCards();
    setupModals();
    setupContextMenu();
    setupFab();
    setupIdentifierModal();
    setupDetailModal();
    setupSearch();
    setupLightbox();
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
async function loadCards() {
    try {
        const res = await authFetch("/api/users/me/cards");
        if (!res.ok) throw new Error();
        cards = await res.json();
        renderCards();
    } catch {
        container.innerHTML = '<div class="empty-state">Не удалось загрузить карты</div>';
    }
}

// ── Search ──
function setupSearch() {
    const input = document.getElementById("cards-search");
    let debounceTimer;
    input.addEventListener("input", () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            searchFilter = input.value.trim().toLowerCase();
            renderCards();
        }, 200);
    });
}

// ── Render ──
function renderCards() {
    container.innerHTML = "";

    const filtered = searchFilter
        ? cards.filter(c => c.name.toLowerCase().includes(searchFilter))
        : cards;

    if (filtered.length === 0) {
        container.innerHTML = searchFilter
            ? '<div class="empty-state"><p>Ничего не найдено</p></div>'
            : `<div class="empty-state">
                <div class="empty-state__icon">&#x1F4B3;</div>
                <p>Нет скидочных карт</p>
                <p class="empty-state__hint">Нажмите + чтобы добавить</p>
              </div>`;
        return;
    }

    for (const card of filtered) {
        const el = document.createElement("div");
        el.className = "item-card" + (!card.isActive ? " item-card--checked" : "");
        el.dataset.id = card.id;

        const badge = card.isActive
            ? '<span class="card-badge card-badge--active">Активна</span>'
            : '<span class="card-badge card-badge--inactive">Неактивна</span>';

        const idCount = card.identifiers ? card.identifiers.length : 0;
        const hasImage = card.identifiers && card.identifiers.some(i => i.imagePath);
        const imageIndicator = hasImage ? '<span class="card-image-indicator" title="Есть изображение">&#x1F5BC;</span>' : '';

        const commentLine = card.comment
            ? `<span class="item-comment">${escapeHtml(card.comment)}</span>`
            : '';

        const metaLine = `<span class="item-comment">${badge} ${imageIndicator} ${idCount} ид.</span>`;

        el.innerHTML = `
            <div class="item-info">
                <span class="item-name">${escapeHtml(card.name)}</span>
                ${commentLine}
                ${metaLine}
            </div>
            <button class="item-menu-btn" data-id="${card.id}" aria-label="Меню">&#x22EE;</button>
        `;

        // Click on card → open lightbox with first image
        el.addEventListener("click", (e) => {
            if (e.target.closest(".item-menu-btn")) return;
            if (hasImage) {
                const firstImage = card.identifiers.find(i => i.imagePath);
                openLightbox("/" + firstImage.imagePath);
            }
        });

        container.appendChild(el);
    }
}

// ── Lightbox ──
function setupLightbox() {
    const overlay = document.getElementById("lightbox");
    document.getElementById("lightbox-close").addEventListener("click", closeLightbox);
    overlay.addEventListener("click", (e) => {
        if (e.target === overlay) closeLightbox();
    });
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && overlay.classList.contains("lightbox--visible")) {
            closeLightbox();
        }
    });
}

function openLightbox(src) {
    document.getElementById("lightbox-img").src = src;
    document.getElementById("lightbox").classList.add("lightbox--visible");
    document.body.classList.add("no-scroll");
}

function closeLightbox() {
    document.getElementById("lightbox").classList.remove("lightbox--visible");
    document.getElementById("lightbox-img").src = "";
    document.body.classList.remove("no-scroll");
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

    document.getElementById("ctx-detail").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        openDetailModal(contextTarget);
    });

    document.getElementById("ctx-edit").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        const card = cards.find(c => c.id === contextTarget);
        if (card) openCardModal(card);
    });

    document.getElementById("ctx-toggle").addEventListener("click", async () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        try {
            const res = await authFetch(`/api/cards/${contextTarget}/toggle`, { method: "PATCH" });
            if (!res.ok) throw new Error();
            showToast("Статус изменён");
            await loadCards();
        } catch {
            showToast("Ошибка изменения статуса", true);
        }
    });

    document.getElementById("ctx-delete").addEventListener("click", () => {
        menu.classList.remove("context-menu--visible");
        if (!contextTarget) return;
        const card = cards.find(c => c.id === contextTarget);
        if (!card) return;
        document.getElementById("confirm-text").textContent =
            `Карта «${card.name}» и все идентификаторы будут удалены.`;
        openModal("confirm-modal");
    });

    document.getElementById("btn-confirm-delete").addEventListener("click", async () => {
        closeModal("confirm-modal");
        if (!contextTarget) return;
        try {
            const res = await authFetch(`/api/cards/${contextTarget}`, { method: "DELETE" });
            if (!res.ok) throw new Error();
            showToast("Карта удалена");
            await loadCards();
        } catch {
            showToast("Ошибка удаления", true);
        }
    });
}

// ── FAB ──
function setupFab() {
    document.getElementById("fab-add").addEventListener("click", () => {
        openCardModal(null);
    });
}

// ── Card modal (create/edit) ──
function openCardModal(card) {
    editingId = card ? card.id : null;
    document.getElementById("card-modal-title").textContent =
        card ? "Редактировать карту" : "Новая карта";
    document.getElementById("card-name").value = card ? card.name : "";
    document.getElementById("card-comment").value = card ? (card.comment || "") : "";
    openModal("card-modal");
    setTimeout(() => document.getElementById("card-name").focus(), 100);
}

document.getElementById("btn-save-card").addEventListener("click", async () => {
    const name = document.getElementById("card-name").value.trim();
    const comment = document.getElementById("card-comment").value.trim() || null;

    if (!name) { showToast("Введите название", true); return; }

    try {
        let res;
        if (editingId) {
            res = await authFetch(`/api/cards/${editingId}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name, comment })
            });
        } else {
            res = await authFetch("/api/cards", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name, comment })
            });
        }

        if (!res.ok) throw new Error();
        closeModal("card-modal");
        showToast(editingId ? "Карта обновлена" : "Карта создана");
        await loadCards();
    } catch {
        showToast("Ошибка сохранения", true);
    }
});

// ── Detail modal (identifiers) ──
function setupDetailModal() {
    document.getElementById("btn-add-identifier").addEventListener("click", () => {
        if (!detailCardId) return;
        closeModal("detail-modal");
        openIdentifierModal();
    });
}

async function openDetailModal(cardId) {
    detailCardId = cardId;
    try {
        const res = await authFetch(`/api/cards/${cardId}`);
        if (!res.ok) throw new Error();
        const card = await res.json();

        document.getElementById("detail-title").textContent = card.name;
        const body = document.getElementById("detail-body");

        if (!card.identifiers || card.identifiers.length === 0) {
            body.innerHTML = '<p class="empty-state__hint">Нет идентификаторов</p>';
        } else {
            body.innerHTML = card.identifiers.map(id => {
                const imgThumb = id.imagePath
                    ? `<img src="/${escapeHtml(id.imagePath)}" class="identifier-thumb" alt="img">`
                    : '';
                return `
                <div class="identifier-row">
                    <span class="identifier-type">${IDENTIFIER_TYPES[id.type] || "?"}</span>
                    <span class="identifier-value">${escapeHtml(id.value)}</span>
                    ${imgThumb}
                    <button class="identifier-delete" data-id="${id.id}" aria-label="Удалить">&times;</button>
                </div>
            `;
            }).join("");

            // Click on thumbnail → lightbox
            body.querySelectorAll(".identifier-thumb").forEach(img => {
                img.addEventListener("click", () => openLightbox(img.src));
            });

            body.querySelectorAll(".identifier-delete").forEach(btn => {
                btn.addEventListener("click", async () => {
                    try {
                        const res = await authFetch(`/api/identifiers/${btn.dataset.id}`, { method: "DELETE" });
                        if (!res.ok) throw new Error();
                        showToast("Идентификатор удалён");
                        await openDetailModal(cardId);
                        await loadCards();
                    } catch {
                        showToast("Ошибка удаления", true);
                    }
                });
            });
        }

        openModal("detail-modal");
    } catch {
        showToast("Ошибка загрузки карты", true);
    }
}

// ── Identifier modal ──
function openIdentifierModal() {
    document.getElementById("identifier-value").value = "";
    document.getElementById("identifier-type").value = "0";
    selectedImageFile = null;
    resetDropZone();
    updateDropZoneVisibility();
    openModal("identifier-modal");
    setTimeout(() => document.getElementById("identifier-value").focus(), 100);
}

function setupIdentifierModal() {
    const typeSelect = document.getElementById("identifier-type");
    const dropZone = document.getElementById("drop-zone");
    const fileInput = document.getElementById("identifier-image");
    const preview = document.getElementById("drop-zone-preview");
    const prompt = document.getElementById("drop-zone-prompt");

    typeSelect.addEventListener("change", updateDropZoneVisibility);

    // Click to browse
    dropZone.addEventListener("click", () => fileInput.click());

    fileInput.addEventListener("change", () => {
        if (fileInput.files && fileInput.files[0]) {
            setDropZoneFile(fileInput.files[0]);
        }
    });

    // Drag-and-drop
    dropZone.addEventListener("dragover", (e) => {
        e.preventDefault();
        dropZone.classList.add("drop-zone--hover");
    });
    dropZone.addEventListener("dragleave", () => {
        dropZone.classList.remove("drop-zone--hover");
    });
    dropZone.addEventListener("drop", (e) => {
        e.preventDefault();
        dropZone.classList.remove("drop-zone--hover");
        if (e.dataTransfer.files && e.dataTransfer.files[0]) {
            setDropZoneFile(e.dataTransfer.files[0]);
        }
    });

    // Save identifier
    document.getElementById("btn-save-identifier").addEventListener("click", async () => {
        const type = parseInt(typeSelect.value);
        const value = document.getElementById("identifier-value").value.trim();

        if (!value) { showToast("Введите значение", true); return; }

        try {
            const fd = new FormData();
            fd.append("type", type);
            fd.append("value", value);
            if (selectedImageFile) {
                fd.append("image", selectedImageFile);
            }

            const res = await authFetch(`/api/cards/${detailCardId}/identifiers`, {
                method: "POST",
                body: fd
            });

            if (res.status === 409) {
                showToast("Такой идентификатор уже используется", true);
                return;
            }
            if (!res.ok) throw new Error();

            closeModal("identifier-modal");
            showToast("Идентификатор добавлен");
            await openDetailModal(detailCardId);
            await loadCards();
        } catch {
            showToast("Ошибка сохранения", true);
        }
    });
}

function updateDropZoneVisibility() {
    const type = parseInt(document.getElementById("identifier-type").value);
    const dropZone = document.getElementById("drop-zone");
    dropZone.style.display = (type !== 0) ? "flex" : "none";
}

function setDropZoneFile(file) {
    if (!file.type.startsWith("image/")) {
        showToast("Выберите изображение", true);
        return;
    }
    selectedImageFile = file;
    const preview = document.getElementById("drop-zone-preview");
    const prompt = document.getElementById("drop-zone-prompt");
    const reader = new FileReader();
    reader.onload = (e) => {
        preview.src = e.target.result;
        preview.style.display = "block";
        prompt.style.display = "none";
    };
    reader.readAsDataURL(file);
}

function resetDropZone() {
    const preview = document.getElementById("drop-zone-preview");
    const prompt = document.getElementById("drop-zone-prompt");
    const fileInput = document.getElementById("identifier-image");
    preview.src = "";
    preview.style.display = "none";
    prompt.style.display = "block";
    fileInput.value = "";
    selectedImageFile = null;
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

// ── Auth guard ──
const token = localStorage.getItem("token");
if (!token) window.location.href = "login.html";

const API = "/api/users/me";
const headers = () => ({
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
});

// ── DOM ──
const loading = document.getElementById("loading");
const content = document.getElementById("profile-content");
const googleBadge = document.getElementById("google-badge");

const inputName = document.getElementById("input-name");
const inputEmail = document.getElementById("input-email");
const inputPwd = document.getElementById("input-password");
const inputPwdConfirm = document.getElementById("input-password-confirm");
const btnRemoveEmail = document.getElementById("btn-remove-email");

// ── Load profile ──
async function loadProfile() {
    try {
        const res = await fetch(API, { headers: headers() });
        if (res.status === 401) { localStorage.clear(); window.location.href = "login.html"; return; }
        if (!res.ok) throw new Error();

        const data = await res.json();
        inputName.value = data.name;
        inputEmail.value = data.email || "";

        if (data.hasGoogle) googleBadge.style.display = "flex";
        if (data.email) btnRemoveEmail.style.display = "inline-block";

        loading.style.display = "none";
        content.style.display = "block";
    } catch {
        loading.textContent = "Ошибка загрузки профиля";
    }
}

loadProfile();

// ── Helpers ──
function showMsg(id, text, isError) {
    const el = document.getElementById(id);
    el.textContent = text;
    el.className = "profile-msg " + (isError ? "profile-msg--error" : "profile-msg--ok");
    setTimeout(() => { el.textContent = ""; el.className = "profile-msg"; }, 3000);
}

function showToast(msg, isError) {
    const t = document.getElementById("toast");
    t.textContent = msg;
    t.className = "toast toast--visible" + (isError ? " toast--error" : "");
    setTimeout(() => { t.className = "toast"; }, 2500);
}

// ── Save name ──
document.getElementById("btn-save-name").addEventListener("click", async () => {
    const name = inputName.value.trim();
    if (!name) { showMsg("msg-name", "Имя не может быть пустым", true); return; }

    try {
        const res = await fetch(`${API}/name`, {
            method: "PUT", headers: headers(),
            body: JSON.stringify({ name })
        });

        if (res.status === 409) { showMsg("msg-name", "Это имя уже занято", true); return; }
        if (!res.ok) throw new Error();

        const data = await res.json();
        inputName.value = data.name;
        localStorage.setItem("userName", data.name);
        showMsg("msg-name", "Имя обновлено", false);
    } catch {
        showMsg("msg-name", "Ошибка сохранения", true);
    }
});

// ── Save email ──
document.getElementById("btn-save-email").addEventListener("click", async () => {
    const email = inputEmail.value.trim();
    if (!email) { showMsg("msg-email", "Введите email", true); return; }

    try {
        const res = await fetch(`${API}/email`, {
            method: "PUT", headers: headers(),
            body: JSON.stringify({ email })
        });

        if (res.status === 409) { showMsg("msg-email", "Этот email уже используется", true); return; }
        if (!res.ok) throw new Error();

        const data = await res.json();
        inputEmail.value = data.email || "";
        btnRemoveEmail.style.display = data.email ? "inline-block" : "none";
        showMsg("msg-email", "Email обновлён", false);
    } catch {
        showMsg("msg-email", "Ошибка сохранения", true);
    }
});

// ── Remove email ──
btnRemoveEmail.addEventListener("click", async () => {
    try {
        const res = await fetch(`${API}/email`, {
            method: "PUT", headers: headers(),
            body: JSON.stringify({ email: null })
        });

        if (!res.ok) throw new Error();

        inputEmail.value = "";
        btnRemoveEmail.style.display = "none";
        showMsg("msg-email", "Email удалён", false);
    } catch {
        showMsg("msg-email", "Ошибка удаления", true);
    }
});

// ── Change password ──
document.getElementById("btn-save-password").addEventListener("click", async () => {
    const password = inputPwd.value;
    const confirmPassword = inputPwdConfirm.value;

    if (!password || !confirmPassword) { showMsg("msg-password", "Заполните оба поля", true); return; }
    if (password !== confirmPassword) { showMsg("msg-password", "Пароли не совпадают", true); return; }
    if (password.length < 4) { showMsg("msg-password", "Минимум 4 символа", true); return; }

    try {
        const res = await fetch(`${API}/password`, {
            method: "PUT", headers: headers(),
            body: JSON.stringify({ password, confirmPassword })
        });

        if (res.status === 400) {
            const text = await res.text();
            showMsg("msg-password", text || "Ошибка", true);
            return;
        }
        if (!res.ok) throw new Error();

        inputPwd.value = "";
        inputPwdConfirm.value = "";
        showMsg("msg-password", "Пароль изменён", false);
    } catch {
        showMsg("msg-password", "Ошибка сохранения", true);
    }
});

// ── Mode toggle ──
const loginSection = document.getElementById("login-section");
const registerSection = document.getElementById("register-section");

document.getElementById("show-register").addEventListener("click", () => {
    loginSection.style.display = "none";
    registerSection.style.display = "block";
    clearErrors();
});

document.getElementById("show-login").addEventListener("click", () => {
    registerSection.style.display = "none";
    loginSection.style.display = "block";
    clearErrors();
});

function clearErrors() {
    document.querySelectorAll(".auth-error").forEach(el => {
        el.textContent = "";
        el.className = "auth-error";
    });
}

function showError(id, msg) {
    const el = document.getElementById(id);
    el.textContent = msg;
    el.className = "auth-error auth-error--visible";
}

// ── If already logged in, redirect ──
if (localStorage.getItem("token")) {
    window.location.href = "index.html";
}

// ── Login ──
document.getElementById("btn-login").addEventListener("click", async () => {
    const name = document.getElementById("login-name").value.trim();
    const password = document.getElementById("login-password").value;

    if (!name || !password) {
        showError("login-error", "Заполните все поля");
        return;
    }

    try {
        const res = await fetch("/api/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name, password })
        });

        if (res.status === 404) {
            showError("login-error", "Пользователь не найден");
            return;
        }
        if (res.status === 401) {
            showError("login-error", "Неверный пароль");
            return;
        }
        if (!res.ok) throw new Error();

        const data = await res.json();
        localStorage.setItem("token", data.token);
        localStorage.setItem("userId", data.userId);
        localStorage.setItem("userName", data.userName);
        window.location.href = "index.html";
    } catch {
        showError("login-error", "Ошибка входа");
    }
});

// ── Register ──
document.getElementById("btn-register").addEventListener("click", async () => {
    const name = document.getElementById("register-name").value.trim();
    const password = document.getElementById("register-password").value;
    const confirm = document.getElementById("register-confirm").value;

    if (!name || !password || !confirm) {
        showError("register-error", "Заполните все поля");
        return;
    }

    if (password !== confirm) {
        showError("register-error", "Пароли не совпадают");
        return;
    }

    if (password.length < 4) {
        showError("register-error", "Минимум 4 символа");
        return;
    }

    try {
        const res = await fetch("/api/auth/register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name, password })
        });

        if (res.status === 409) {
            showError("register-error", "Пользователь с таким именем уже существует");
            return;
        }
        if (!res.ok) throw new Error();

        const data = await res.json();
        localStorage.setItem("token", data.token);
        localStorage.setItem("userId", data.userId);
        localStorage.setItem("userName", data.userName);
        window.location.href = "index.html";
    } catch {
        showError("register-error", "Ошибка регистрации");
    }
});

// ── Submit on Enter ──
document.getElementById("login-password").addEventListener("keydown", (e) => {
    if (e.key === "Enter") document.getElementById("btn-login").click();
});

document.getElementById("register-confirm").addEventListener("keydown", (e) => {
    if (e.key === "Enter") document.getElementById("btn-register").click();
});

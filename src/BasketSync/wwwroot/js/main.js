document.addEventListener("DOMContentLoaded", () =>
{
    loadShoppingLists();
    setupAddListButton();
});

async function loadShoppingLists()
{
    const listContainer = document.getElementById("lists-container");
    try {
        const res = await fetch("/api/users/1/lists");

        if (!res.ok)
            throw new Error("Ошибка при загрузке списков");

        const lists = await res.json();
        for (const list of lists)
        {
            const li = createListElement(list);
            listContainer.appendChild(li);
        }
    }
    catch (err) {
        alert("Ошибка: " + err.message);
    }
}

function createListElement(list) {
    const li = document.createElement("li");
    li.classList.add("list-item");

    const nameSpan = document.createElement("span");
    nameSpan.textContent = list.name;
    nameSpan.classList.add("list-name");
    nameSpan.style.cursor = "pointer";

    nameSpan.addEventListener("click", () => {
        window.location.href = `list.html?id=${list.id}`;
    });

    li.appendChild(nameSpan);
    return li;
}

function setupAddListButton()
{
    const button = document.getElementById("addListBtn");
    const listContainer = document.getElementById("lists-container");

    button.addEventListener("click", async () => {
        const name = prompt("Введите название списка:");
        if (!name) return;

        try {
            const res = await fetch("/api/lists", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name: name, userId: 1 })
            });

            if (!res.ok)
                throw new Error(await res.text());

            const newList = await res.json();
            const li = createListElement(newList);
            listContainer.appendChild(li);
        }
        catch (err) {
            alert("Ошибка: " + err.message);
        }
    });
}

document.addEventListener("DOMContentLoaded", () => 
{
    loadShoppingLists();
    setupAddListButton
});

async function loadShoppingLists() 
{
    const listContainer = document.getElementById("listContainer");
    try {
        const res = await fetch("https://localhost:7011/api/users/1/lists");

        if (!res.ok)
            throw new Error("Ошибка при загрузке списков");

        const lists = await res.json();
        for (const list of lists)
        {
            const li = document.createElement("li");
            li.textContent = list.name;
            listContainer.appendChild(li);
        }
    }
    catch (err) {
        alert("Ошибка: " + err.message);
    }
}

function setupAddListButton()
{
    const button = document.getElementById("addListBtn");
    const listContainer = document.getElementById("listContainer");

    button.addEventListener("click", async () => {
        const name = prompt("Введите название списка:");
        if (!name) return;

        try {
            const res = await fetch("https://localhost:7011/api/lists", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ name: name, userId: 1 })
            });

            if (!res.ok)
                throw new Error(await res.text())

            const newList = res.json();
            const li = document.createElement("li");
            li.textContent = newList.name;
            listContainer.appendChild(li);
        }
        catch (err) {
            const text = await err.response?.text?.();
            alert("Ошибка: " + (text || err.message));
        }
    });
}
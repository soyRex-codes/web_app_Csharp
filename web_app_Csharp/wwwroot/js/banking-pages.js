function showProblem(element, problem) {
    const errors = problem.errors
        ? Object.values(problem.errors).flat().join(" ")
        : problem.detail ?? problem.title ?? "The request could not be completed.";

    element.textContent = errors;
    element.hidden = false;
}

async function readProblem(response) {
    try {
        return await response.json();
    } catch {
        return { title: "The request could not be completed." };
    }
}

document.querySelectorAll("[data-api-form]").forEach((form) => {
    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        const error = form.querySelector("[data-form-error]");
        error.hidden = true;
        const data = Object.fromEntries(new FormData(form));
        const response = await fetch(form.dataset.apiEndpoint, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data)
        });

        if (response.ok) {
            window.location.assign(form.dataset.successRedirect);
            return;
        }

        showProblem(error, await readProblem(response));
    });
});

document.querySelectorAll("[data-logout]").forEach((button) => {
    button.addEventListener("click", async () => {
        await fetch("/api/v1/auth/logout", { method: "POST" });
        window.location.assign("/");
    });
});

document.querySelectorAll("[data-accounts-table]").forEach(async (table) => {
    const error = document.querySelector("[data-accounts-error]");
    const emptyState = document.querySelector("[data-empty-state]");
    const list = table.querySelector("[data-accounts-list]");
    const showOwner = table.dataset.showOwner === "true";
    const response = await fetch("/api/v1/accounts");

    if (!response.ok) {
        showProblem(error, await readProblem(response));
        return;
    }

    const accounts = await response.json();
    if (accounts.length === 0) {
        emptyState.hidden = false;
        table.hidden = true;
        return;
    }

    const money = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" });
    accounts.forEach((account) => {
        const row = document.createElement("tr");
        [account.name, account.type, money.format(account.balance)]
            .forEach((value) => {
                const cell = document.createElement("td");
                cell.textContent = value;
                row.append(cell);
            });

        if (showOwner) {
            const owner = document.createElement("td");
            owner.textContent = account.ownerId;
            row.append(owner);
        }

        list.append(row);
    });
});

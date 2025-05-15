// För _Layoutsidan <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>
// DarkMode / LightMode från bootstrap med localStorage och har extra finesser <<<>>>>>
document.addEventListener("DOMContentLoaded", function () {
    const savedTheme = localStorage.getItem("theme") || "light";
    document.documentElement.setAttribute("data-bs-theme", savedTheme);
    const toggleButton = document.getElementById("darkModeToggle");

    //Laddar finesser
    updateToggleButtonText(savedTheme);
    updateThemeDependentLinks(savedTheme);
});

function toggleDarkMode() {
    const currentTheme = document.documentElement.getAttribute("data-bs-theme");
    const newTheme = currentTheme === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-bs-theme", newTheme);
    localStorage.setItem("theme", newTheme);

    // Laddar finesser
    updateToggleButtonText(newTheme);
    updateThemeDependentLinks(newTheme);
}

// Extra finesser.
function updateToggleButtonText(theme) {
    const button = document.getElementById("darkModeToggle");
    if (button) {
        button.textContent = theme === "dark" ? "☀️ Ljust läge" : "🌙 Mörkt läge";
    }
}

function updateThemeDependentLinks(theme) {
    const links = document.querySelectorAll(".theme-dependent");
    links.forEach(link => {
        link.style.color = theme === "dark" ? "white" : "black";
    });
}

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>

// För att gå tillbaka till föregående sida
function reloadCurrentPage() {
    window.location.href = window.location.pathname;
}

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>

// För att scrolla till editFormSection om editId finns i URL:en på Admin sidan
window.addEventListener('DOMContentLoaded', function () {
    const params = new URLSearchParams(window.location.search);
    if (params.has('editId')) {
        const editForm = document.getElementById('editFormSection');
        if (editForm) {
            editForm.scrollIntoView({ behavior: 'smooth' });
        }
    }
});
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

    // För att gå tillbaka till föregående sida med id
    function reloadCurrentPage(id) {
        window.location.href = window.location.pathname + "?reloadId=" + id;
    }


//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>

// Funktion för att scrolla till ett element om en query-param finns i URL:en
function scrollToSectionIfParamExists(paramName, options = {}) {
    const params = new URLSearchParams(window.location.search);
    if (!paramName || !params.has(paramName)) return;

    const value = params.get(paramName);
    let section = null;

    if (options.prefix) {
        section = document.getElementById(options.prefix + "-" + value);
    } else if (options.id) {
        section = document.getElementById(options.id);
    }

    if (section) {
        section.scrollIntoView({ behavior: 'smooth' });
    }
}

window.addEventListener('DOMContentLoaded', function () {
    scrollToSectionIfParamExists('showId', { id: 'bloggTopSection' });
    scrollToSectionIfParamExists('editId', { id: 'editFormSection' });
    scrollToSectionIfParamExists('reloadId', { prefix: 'reloadPageFormSection' });

    // Lägg till fler anrop här om du vill stödja fler parametrar/element
    // scrollToSectionIfParamExists('anotherParam', { id/prefix: 'anotherSectionId'});
});

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
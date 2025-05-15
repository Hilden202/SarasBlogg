// För _Layoutsidan <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>
// DarkMode / LightMode med localStorage
document.addEventListener("DOMContentLoaded", function () {
	const savedTheme = localStorage.getItem("theme") || "light";
	document.documentElement.setAttribute("data-bs-theme", savedTheme);
	updateToggleButtonText(savedTheme);
	updateThemeDependentLinks(savedTheme);
});

function toggleDarkMode() {
	const currentTheme = document.documentElement.getAttribute("data-bs-theme");
	const newTheme = currentTheme === "dark" ? "light" : "dark";
	document.documentElement.setAttribute("data-bs-theme", newTheme);
	localStorage.setItem("theme", newTheme);
	updateToggleButtonText(newTheme);
	updateThemeDependentLinks(newTheme);
}

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
function showAll() {
	window.location.href = window.location.pathname;
}

//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>

// För Privacy eventuellt Admin sidan <<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
// Detta är ett enkelt skript för att öppna en modal när en knapp klickas
document.addEventListener('DOMContentLoaded', function () {
    var openFormBtn = document.getElementById('openFormBtn');
    if (openFormBtn) {
        openFormBtn.addEventListener('click', function () {
            var modal = new bootstrap.Modal(document.getElementById('aboutMeFormModal'));
            modal.show();
        });
    }
});
//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var menu = document.getElementById("customerNav");
        var toggle = document.querySelector('[data-bs-target="#customerNav"]');
        if (!menu || !toggle) return;

        menu.addEventListener("shown.bs.collapse", function () {
            toggle.setAttribute("aria-label", "Đóng menu");
        });
        menu.addEventListener("hidden.bs.collapse", function () {
            toggle.setAttribute("aria-label", "Mở menu");
        });
    });
})();


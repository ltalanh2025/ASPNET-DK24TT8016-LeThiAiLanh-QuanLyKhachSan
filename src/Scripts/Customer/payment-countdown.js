(function () {
    "use strict";

    function formatRemaining(milliseconds) {
        var totalSeconds = Math.max(0, Math.floor(milliseconds / 1000));
        var hours = Math.floor(totalSeconds / 3600);
        var minutes = Math.floor((totalSeconds % 3600) / 60);
        var seconds = totalSeconds % 60;
        return [hours, minutes, seconds].map(function (value) { return String(value).padStart(2, "0"); }).join(":");
    }

    document.addEventListener("DOMContentLoaded", function () {
        Array.prototype.forEach.call(document.querySelectorAll("[data-payment-countdown]"), function (container) {
            var deadline = new Date(container.getAttribute("data-deadline"));
            var value = container.querySelector("[data-countdown-value]");
            if (!value || isNaN(deadline.getTime())) return;

            var timer;
            var update = function () {
                var remaining = deadline.getTime() - Date.now();
                if (remaining <= 0) {
                    value.textContent = "Đã hết hạn — vui lòng tải lại trang";
                    container.classList.add("is-expired");
                    if (timer) window.clearInterval(timer);
                    return;
                }
                value.textContent = formatRemaining(remaining);
            };

            update();
            timer = window.setInterval(update, 1000);
        });
    });
})();

(function () {
    "use strict";

    function addOneDay(value) {
        var parts = (value || "").split("-");
        if (parts.length !== 3) return "";
        var date = new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
        if (isNaN(date.getTime())) return "";
        date.setDate(date.getDate() + 1);
        var month = String(date.getMonth() + 1).padStart(2, "0");
        var day = String(date.getDate()).padStart(2, "0");
        return date.getFullYear() + "-" + month + "-" + day;
    }

    document.addEventListener("DOMContentLoaded", function () {
        Array.prototype.forEach.call(document.querySelectorAll("[data-booking-search]"), function (form) {
            var checkIn = form.querySelector("[data-check-in]");
            var checkOut = form.querySelector("[data-check-out]");
            if (!checkIn || !checkOut) return;

            var synchronizeDates = function () {
                var minimumCheckout = addOneDay(checkIn.value);
                if (!minimumCheckout) return;
                checkOut.min = minimumCheckout;
                if (!checkOut.value || checkOut.value < minimumCheckout) checkOut.value = minimumCheckout;
            };

            checkIn.addEventListener("change", synchronizeDates);
            synchronizeDates();
        });
    });
})();


(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        Array.prototype.forEach.call(document.querySelectorAll("img[data-image-fallback]"), function (image) {
            image.addEventListener("error", function handleError() {
                image.removeEventListener("error", handleError);
                image.src = image.getAttribute("data-image-fallback");
            });
        });
    });
})();

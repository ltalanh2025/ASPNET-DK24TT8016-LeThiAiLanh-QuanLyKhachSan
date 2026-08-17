(function () {
    "use strict";

    function byId(id) { return document.getElementById(id); }

    function setSidebar(open) {
        var sidebar = byId("appSidebar");
        var backdrop = byId("sidebarBackdrop");
        var toggle = byId("sidebarToggle");
        if (!sidebar || !backdrop) return;

        sidebar.classList.toggle("is-open", open);
        backdrop.classList.toggle("is-visible", open);
        document.body.classList.toggle("sidebar-open", open);
        if (toggle) toggle.setAttribute("aria-expanded", open ? "true" : "false");
    }

    function formIsValid(form) {
        if (typeof form.checkValidity === "function" && !form.checkValidity()) return false;
        if (window.jQuery && window.jQuery.fn && window.jQuery.fn.valid && window.jQuery(form).data("validator")) {
            return window.jQuery(form).valid();
        }
        return true;
    }

    function setLoading(form) {
        if (form.dataset.submitting === "true") return false;
        form.dataset.submitting = "true";
        var buttons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
        Array.prototype.forEach.call(buttons, function (button) {
            button.disabled = true;
            if (button.tagName === "BUTTON") {
                button.dataset.originalHtml = button.innerHTML;
                var text = button.getAttribute("data-loading-text") || "Đang xử lý...";
                button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>' + text;
            }
        });
        return true;
    }

    function openConfirmModal(form) {
        var modalElement = byId("confirmActionModal");
        if (!modalElement || !window.bootstrap || !window.bootstrap.Modal) return false;

        var title = byId("confirmActionTitle");
        var message = byId("confirmActionMessage");
        var target = byId("confirmActionTarget");
        var confirmButton = byId("confirmActionButton");

        title.textContent = form.getAttribute("data-confirm-title") || "Xác nhận thao tác";
        message.textContent = form.getAttribute("data-confirm-message") || "Bạn có chắc chắn muốn tiếp tục?";
        var targetText = form.getAttribute("data-confirm-target") || "";
        target.textContent = targetText;
        target.hidden = !targetText;
        confirmButton.textContent = form.getAttribute("data-confirm-button") || "Xác nhận";
        confirmButton.className = "btn " + (form.getAttribute("data-confirm-class") || "btn-danger");

        confirmButton.onclick = function () {
            var modal = window.bootstrap.Modal.getOrCreateInstance(modalElement);
            modal.hide();
            if (setLoading(form)) HTMLFormElement.prototype.submit.call(form);
        };

        window.bootstrap.Modal.getOrCreateInstance(modalElement).show();
        return true;
    }

    document.addEventListener("DOMContentLoaded", function () {
        var toggle = byId("sidebarToggle");
        var close = byId("sidebarClose");
        var backdrop = byId("sidebarBackdrop");
        if (toggle) toggle.addEventListener("click", function () { setSidebar(true); });
        if (close) close.addEventListener("click", function () { setSidebar(false); });
        if (backdrop) backdrop.addEventListener("click", function () { setSidebar(false); });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") setSidebar(false);
        });

        Array.prototype.forEach.call(document.querySelectorAll("[data-password-toggle]"), function (button) {
            button.addEventListener("click", function () {
                var input = byId(button.getAttribute("data-password-toggle"));
                if (!input) return;
                var show = input.type === "password";
                input.type = show ? "text" : "password";
                button.setAttribute("aria-label", show ? "Ẩn mật khẩu" : "Hiện mật khẩu");
                button.setAttribute("aria-pressed", show ? "true" : "false");
                button.textContent = show ? "Ẩn" : "Hiện";
                input.focus();
            });
        });

        Array.prototype.forEach.call(document.querySelectorAll("[data-caps-lock-target]"), function (input) {
            var warning = byId(input.getAttribute("data-caps-lock-target"));
            if (!warning) return;
            var update = function (event) {
                var isOn = event.getModifierState && event.getModifierState("CapsLock");
                warning.classList.toggle("is-visible", !!isOn);
            };
            input.addEventListener("keydown", update);
            input.addEventListener("keyup", update);
            input.addEventListener("blur", function () { warning.classList.remove("is-visible"); });
        });

        Array.prototype.forEach.call(document.querySelectorAll("[data-auto-dismiss]"), function (alert) {
            window.setTimeout(function () {
                if (window.bootstrap && window.bootstrap.Alert) window.bootstrap.Alert.getOrCreateInstance(alert).close();
            }, 6000);
        });

        Array.prototype.forEach.call(document.querySelectorAll("[data-history-back]"), function (button) {
            button.addEventListener("click", function () {
                if (window.history.length > 1) window.history.back();
                else window.location.href = button.getAttribute("data-history-fallback") || "/";
            });
        });

        Array.prototype.forEach.call(document.querySelectorAll("[data-print-page]"), function (button) {
            button.addEventListener("click", function () { window.print(); });
        });

        Array.prototype.forEach.call(document.querySelectorAll("[data-filter-select]"), function (input) {
            var select = byId(input.getAttribute("data-filter-select"));
            if (!select) return;
            var options = Array.prototype.map.call(select.options, function (option) {
                return { value: option.value, text: option.text, disabled: option.disabled };
            });
            input.addEventListener("input", function () {
                var selectedValue = select.value;
                var keyword = input.value.trim().toLocaleLowerCase("vi-VN");
                select.innerHTML = "";
                options.forEach(function (item, index) {
                    if (index !== 0 && keyword && item.text.toLocaleLowerCase("vi-VN").indexOf(keyword) === -1) return;
                    var option = document.createElement("option");
                    option.value = item.value;
                    option.text = item.text;
                    option.disabled = item.disabled;
                    option.selected = item.value === selectedValue;
                    select.appendChild(option);
                });
            });
        });
    });

    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!form || form.tagName !== "FORM" || !formIsValid(form)) return;

        if (form.dataset.submitting === "true") {
            event.preventDefault();
            return;
        }

        if (form.hasAttribute("data-confirm")) {
            event.preventDefault();
            if (!openConfirmModal(form)) window.alert("Không thể mở hộp thoại xác nhận. Vui lòng tải lại trang và thử lại.");
            return;
        }

        setLoading(form);
    });
})();

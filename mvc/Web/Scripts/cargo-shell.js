(function () {
    "use strict";
    var toggle = document.getElementById("menu-toggle");
    if (window.matchMedia("(max-width:700px)").matches) {
        document.body.classList.add("nav-collapsed");
        toggle.setAttribute("aria-expanded", "false");
    }
    toggle.addEventListener("click", function () {
        var closed = document.body.classList.toggle("nav-collapsed");
        toggle.setAttribute("aria-expanded", String(!closed));
    });
    var active = document.querySelector(".nav-item.active");
    if (active) active.parentNode.scrollTop = Math.max(0, active.offsetTop - 300);
    function clock() {
        document.getElementById("clock").textContent = new Date().toLocaleString("zh-TW", { hour12: false });
    }
    clock(); setInterval(clock, 1000);
    var toastTimer;
    window.cargoToast = function (message) {
        var wrap = document.getElementById("toast-wrap");
        if (!wrap) {
            wrap = document.createElement("div");
            wrap.id = "toast-wrap";
            wrap.setAttribute("role", "alert");
            wrap.setAttribute("aria-atomic", "true");
            document.body.appendChild(wrap);
        }
        clearTimeout(toastTimer);
        wrap.textContent = "";
        var toast = document.createElement("div");
        toast.className = "toast err";
        toast.textContent = message;
        wrap.appendChild(toast);
        toastTimer = setTimeout(function () { wrap.textContent = ""; }, 4000);
    };
    window.cargoConfirm = function (message, onConfirm) {
        return window.Swal.fire({
            title: "確認操作",
            text: message,
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "確定",
            cancelButtonText: "取消",
            reverseButtons: false,
            buttonsStyling: false,
            allowOutsideClick: false,
            allowEscapeKey: true,
            focusConfirm: true,
            returnFocus: true,
            heightAuto: false,
            customClass: {
                popup: "cargo-confirm",
                confirmButton: "btn btn-primary",
                cancelButton: "btn btn-ghost"
            }
        }).then(function (result) {
            if (result.isConfirmed && typeof onConfirm === "function") {
                onConfirm();
            }
            return result;
        });
    };
}());

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
    var mask = document.getElementById("confirm-mask"), pending, previousFocus;
    window.cargoConfirm = function (message, onConfirm) {
        pending = onConfirm; previousFocus = document.activeElement;
        document.getElementById("confirm-text").textContent = message;
        mask.hidden = false; mask.classList.add("show");
        document.getElementById("confirm-ok").focus();
    };
    function close(accepted) {
        mask.hidden = true; mask.classList.remove("show");
        var callback = pending; pending = null;
        if (previousFocus) previousFocus.focus();
        if (accepted && callback) callback();
    }
    document.getElementById("confirm-ok").onclick = function () { close(true); };
    document.getElementById("confirm-cancel").onclick = function () { close(false); };
    mask.addEventListener("keydown", function (event) {
        if (event.key === "Escape") { event.preventDefault(); close(false); }
        if (event.key === "Tab") {
            event.preventDefault();
            document.getElementById(document.activeElement.id === "confirm-ok" ? "confirm-cancel" : "confirm-ok").focus();
        }
    });
}());

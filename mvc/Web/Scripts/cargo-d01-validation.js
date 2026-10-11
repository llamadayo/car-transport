(function () {
    "use strict";
    window.cargoValidateApplication = function (form) {
        var required = {
            Applicant: "請填寫申請人", Preference1: "請選擇車種順位 1",
            StartAtDate: "請填寫用車起日", StartAtTime: "請填寫用車起始時間",
            EndAtDate: "請填寫用車迄日", EndAtTime: "請填寫用車結束時間",
            Passengers: "請填寫乘客數", SelfDrive: "請選擇是否自駕"
        };
        function fail(control, message) {
            control.setAttribute("aria-invalid", "true");
            control.focus();
            control.scrollIntoView({ block: "center", behavior: "smooth" });
            window.cargoToast(message);
            return false;
        }
        var controls = form.querySelectorAll("input, select, textarea");
        for (var i = 0; i < controls.length; i++) {
            var control = controls[i];
            if (control.disabled || control.type === "hidden" || control.type === "submit" || control.type === "button") continue;
            control.removeAttribute("aria-invalid");
            var message = required[control.name] || required[control.id];
            if (/^Locations\[\d+\]\.Name$/.test(control.name)) message = "行程地點不可空白，請填寫或刪除該列";
            var cargo = /^Cargo\[\d+\]\.(Name|Length|Width|Height|Weight|Quantity|Category)$/.exec(control.name);
            if (cargo) message = "請填寫隨行貨物" + ({ Name: "品名", Length: "長度", Width: "寬度", Height: "高度", Weight: "重量", Quantity: "件數", Category: "品類" })[cargo[1]];
            message = message || control.getAttribute("data-val-required");
            if (control.type === "radio") {
                if (message && !Array.prototype.some.call(controls, function (item) { return item.type === "radio" && item.name === control.name && item.checked; })) return fail(control, message);
            } else if ((message || control.required) && !control.value.trim()) {
                return fail(control, message || "請填寫此必填欄位");
            }
            if (!control.checkValidity()) {
                var label = control.getAttribute("aria-label") || (control.labels && control.labels.length ? control.labels[0].textContent.trim() : control.name);
                return fail(control, (label || "欄位") + "：" + control.validationMessage);
            }
        }
        var start = form.querySelector('[name="StartAt"]'), end = form.querySelector('[name="EndAt"]');
        if (start && end && start.value && end.value && end.value <= start.value) return fail(form.querySelector("#EndAtDate"), "用車迄時須晚於起時");
        var chosen = [];
        for (var n = 1; n <= 3; n++) {
            var preference = form.querySelector('[name="Preference' + n + '"]');
            if (preference && preference.value) {
                if (chosen.indexOf(preference.value) >= 0) return fail(preference, "車種順位不可重複");
                chosen.push(preference.value);
            }
        }
        return true;
    };
}());

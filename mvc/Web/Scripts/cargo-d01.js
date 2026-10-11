(function () {
    "use strict";
    var form = document.getElementById("application-form");
    if (!form) return;
    // Run validation in the submit handler so all errors use the shared toast.
    form.noValidate = true;
    function reindex(kind) {
        var rows = document.getElementById("rows-" + kind).rows;
        for (var i = 0; i < rows.length; i++) {
            var controls = rows[i].querySelectorAll("[name]");
            for (var j = 0; j < controls.length; j++) {
                controls[j].name = controls[j].name.replace(/\[(?:\d+|__index__)\]/, "[" + i + "]");
                controls[j].removeAttribute("id");
            }
            var sequence = rows[i].querySelector("[data-sequence]");
            if (sequence) sequence.textContent = i + 1;
            var up = rows[i].querySelector('[data-move="-1"]');
            var down = rows[i].querySelector('[data-move="1"]');
            if (up) up.disabled = i === 0;
            if (down) down.disabled = i === rows.length - 1;
        }
    }
    form.addEventListener("click", function (event) {
        var add = event.target.closest("[data-add]");
        if (add) {
            var kind = add.getAttribute("data-add");
            var tbody = document.getElementById("rows-" + kind);
            if (kind === "SelfDrivers" && tbody.rows.length >= 8) { window.alert("自駕人最多 8 筆"); return; }
            tbody.appendChild(document.getElementById("template-" + kind).content.cloneNode(true));
            reindex(kind);
        }
        var move = event.target.closest("[data-move]");
        if (move && !move.disabled) {
            var movingRow = move.closest("tr"), container = movingRow.parentNode;
            var before = move.getAttribute("data-move") === "-1";
            var adjacent = before ? movingRow.previousElementSibling : movingRow.nextElementSibling;
            if (adjacent) {
                container.insertBefore(before ? movingRow : adjacent, before ? adjacent : movingRow);
                reindex(container.id.substring(5));
            }
        }
        var remove = event.target.closest("[data-remove]");
        if (remove) {
            var row = remove.closest("tr"), parent = row.parentNode;
            parent.removeChild(row);
            reindex(parent.id.substring(5));
        }
    });
    function update() {
        document.getElementById("exercise-field").hidden =
            !(document.getElementById("Category").value === "general" && document.getElementById("TransportType").value === "演訓");
        var selfDrive = form.querySelector('[name="SelfDrive"]:checked');
        document.getElementById("self-driver-section").hidden = !selfDrive || selfDrive.value !== "true";
        var review = form.querySelector('[name="RequestReview"]:checked');
        document.getElementById("submit-request").disabled = !review || review.value !== "true";
        form.querySelectorAll('.radio-pill').forEach(function (pill) {
            pill.classList.toggle('sel', pill.querySelector('input').checked);
        });
        ["StartAt", "EndAt"].forEach(function (name) {
            var date = document.getElementById(name + "Date").value;
            var time = document.getElementById(name + "Time").value;
            document.getElementById(name).value = date && time ? date + "T" + time : "";
        });
    }
    form.addEventListener("change", update);
    var lastCommand = "";
    form.addEventListener("click", function (event) {
        if (event.target.name === "command") lastCommand = event.target.value;
    });
    form.addEventListener("submit", function (event) {
        update();
        var command = event.submitter ? event.submitter.value : lastCommand;
        if (command !== "draft" && command !== "submit") { event.preventDefault(); return; }
        event.preventDefault();
        if (!window.cargoValidateApplication(form)) return;
        window.cargoConfirm(command === "draft" ? "確認暫存申請？" : "確認送出申請並進入待二級審？", function () {
            var input = document.createElement("input");
            input.type = "hidden"; input.name = "command"; input.value = command;
            form.appendChild(input);
            HTMLFormElement.prototype.submit.call(form);
        });
    });
    update();
}());

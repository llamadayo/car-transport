(function () {
    'use strict';
    window.cargoFieldError = function (control, message) {
        control.setAttribute('aria-invalid', 'true');
        control.focus();
        control.scrollIntoView({ block: 'center', behavior: 'smooth' });
        window.cargoToast(message);
        return false;
    };
    window.cargoValidateForm = function (form, required) {
        required = required || {};
        var controls = form.querySelectorAll('input, select, textarea');
        for (var i = 0; i < controls.length; i++) {
            var control = controls[i];
            if (control.disabled || /^(hidden|submit|button)$/.test(control.type)) continue;
            control.removeAttribute('aria-invalid');
            var label = control.getAttribute('aria-label') || (control.labels && control.labels.length ? control.labels[0].textContent.trim() : control.name);
            var message = required[control.name];
            if ((message || control.required || control.getAttribute('data-val-required')) && !control.value.trim()) return window.cargoFieldError(control, message || '請填寫' + label);
            if (!control.checkValidity()) return window.cargoFieldError(control, label + '：' + control.validationMessage);
        }
        return true;
    };
}());

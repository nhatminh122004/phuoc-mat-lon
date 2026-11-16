/**
 * CSRF helper dùng chung cho mọi AJAX POST.
 * Token do Razor render trong #csrfTokenHost (xem Views/Shared/_Layout.cshtml).
 * Không có token => request sẽ bị server trả 500 (ValidateAntiForgeryToken),
 * nên mọi POST phải đi qua BloomCsrf.
 */
window.BloomCsrf = (function () {
    'use strict';

    function token() {
        const host = document.getElementById('csrfTokenHost');
        if (host) {
            const input = host.querySelector('input[name="__RequestVerificationToken"]');
            if (input && input.value) {
                return input.value;
            }
        }

        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') : '';
    }

    function withToken(params) {
        const value = token();
        if (value) {
            params.append('__RequestVerificationToken', value);
        }
        return params;
    }

    function headers(extra) {
        const base = {
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json'
        };
        const value = token();
        if (value) {
            base['__RequestVerificationToken'] = value;
        }
        return Object.assign(base, extra || {});
    }

    return { token: token, withToken: withToken, headers: headers };
})();

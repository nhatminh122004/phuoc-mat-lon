/**
 * HỮU PHƯỚC FLORIST - FULL-STACK AUTHENTICATION & USER STATE
 * Synchronizes authentication with ASP.NET MVC AccountController (MySQL + Session/Cookie)
 * and updates Header user profile UI.
 */

const BloomAuth = (function () {
    // Không dùng localStorage: trạng thái đăng nhập lấy từ server (/Account/CurrentUser)
    // để tránh trường hợp UI hiển thị user "giả" trong khi session đã hết hạn.
    let currentUser = null;

    function getUser() {
        return currentUser;
    }

    function setUser(user) {
        currentUser = user || null;
        updateHeaderUser();
    }

    function syncFromServer() {
        fetch('/Account/CurrentUser', {
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
        })
        .then(res => {
            if (!res.ok) throw new Error('Non-JSON');
            return res.json();
        })
        .then(data => {
            if (data && data.authenticated && data.user) {
                setUser(data.user);
            } else if (data && data.authenticated === false) {
                setUser(null);
            }
        })
        .catch(() => {
            updateHeaderUser();
        });
    }

    function logout() {
        const body = window.BloomCsrf
            ? window.BloomCsrf.withToken(new URLSearchParams())
            : new URLSearchParams();

        fetch('/Account/LogoutAjax', {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
            body: body.toString()
        })
        .catch(() => {})
        .finally(() => {
            currentUser = null;
            updateHeaderUser();
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: 'Đã đăng xuất tài khoản!',
                    showConfirmButton: false,
                    timer: 1400
                }).then(() => {
                    window.location.href = '/';
                });
            } else {
                window.location.href = '/';
            }
        });
    }

    function updateHeaderUser() {
        const user = getUser();
        const guestElements = document.querySelectorAll('.js-auth-guest');
        const userElements = document.querySelectorAll('.js-auth-logged');
        const userNameElements = document.querySelectorAll('.js-auth-username');
        const userEmailElements = document.querySelectorAll('.js-auth-useremail');

        if (user) {
            guestElements.forEach(el => el.style.display = 'none');
            userElements.forEach(el => el.style.display = 'block');
            userNameElements.forEach(el => el.textContent = user.name || user.username || 'Quý Khách');
            userEmailElements.forEach(el => el.textContent = user.email || '');
        } else {
            guestElements.forEach(el => el.style.display = 'block');
            userElements.forEach(el => el.style.display = 'none');
        }
    }

    function togglePassword(inputId, btn) {
        const input = document.getElementById(inputId);
        if (!input) return;
        const icon = btn.querySelector('i');
        if (input.type === 'password') {
            input.type = 'text';
            if (icon) {
                icon.classList.remove('fa-eye');
                icon.classList.add('fa-eye-slash');
            }
        } else {
            input.type = 'password';
            if (icon) {
                icon.classList.remove('fa-eye-slash');
                icon.classList.add('fa-eye');
            }
        }
    }

    function showAlert(alertId, message, isSuccess = false) {
        const alertEl = document.getElementById(alertId);
        if (!alertEl) return;
        alertEl.style.display = 'flex';
        alertEl.className = 'auth-alert ' + (isSuccess ? 'auth-alert-success' : 'auth-alert-error');
        alertEl.innerHTML = `
            <i class="fa-solid ${isSuccess ? 'fa-circle-check' : 'fa-circle-exclamation'}"></i>
            <span>${message}</span>
        `;
    }

    function hideAlert(alertId) {
        const alertEl = document.getElementById(alertId);
        if (alertEl) alertEl.style.display = 'none';
    }

    function isValidEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    function handleLogin(e) {
        e.preventDefault();
        hideAlert('loginAlert');

        const emailInput = document.getElementById('loginEmail');
        const passwordInput = document.getElementById('loginPassword');
        const rememberMeInput = document.getElementById('rememberMe');
        const returnUrlInput = document.getElementById('loginReturnUrl');
        const submitBtn = document.getElementById('loginSubmitBtn');

        const email = emailInput ? emailInput.value.trim() : '';
        const password = passwordInput ? passwordInput.value : '';
        const rememberMe = rememberMeInput ? rememberMeInput.checked : true;
        const returnUrl = returnUrlInput && returnUrlInput.value
            ? returnUrlInput.value
            : (new URLSearchParams(window.location.search).get('returnUrl') || '');

        if (!email) {
            showAlert('loginAlert', 'Vui lòng nhập Email hoặc Tên đăng nhập của bạn.');
            if (emailInput) emailInput.focus();
            return;
        }
        if (!password) {
            showAlert('loginAlert', 'Vui lòng nhập mật khẩu.');
            if (passwordInput) passwordInput.focus();
            return;
        }

        submitBtn.disabled = true;
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = `<span class="spinner-btn"></span> Đang xác thực...`;

        const body = new URLSearchParams({
            email: email,
            password: password,
            rememberMe: rememberMe ? 'true' : 'false',
            returnUrl: returnUrl
        });

        fetch('/Account/Login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            },
            body: (window.BloomCsrf ? window.BloomCsrf.withToken(body) : body).toString()
        })
        .then(res => res.json())
        .then(data => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            if (!data.success) {
                showAlert('loginAlert', data.message || 'Đăng nhập không thành công.');
                return;
            }

            setUser(data.user);
            showAlert('loginAlert', 'Đăng nhập thành công! Đang chuyển hướng...', true);

            const targetUrl = data.redirectUrl || returnUrl || '/';
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Chào mừng trở lại!',
                    text: `Xin chào ${data.user.name}, chúc bạn một ngày ngập tràn niềm vui!`,
                    showConfirmButton: false,
                    timer: 1400
                }).then(() => {
                    window.location.href = targetUrl;
                });
            } else {
                setTimeout(() => { window.location.href = targetUrl; }, 1000);
            }
        })
        .catch(() => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
            showAlert('loginAlert', 'Không thể kết nối máy chủ xác thực. Vui lòng thử lại.');
        });
    }

    function handleRegister(e) {
        e.preventDefault();
        hideAlert('registerAlert');

        const fullName = document.getElementById('regFullName').value.trim();
        const email = document.getElementById('regEmail').value.trim();
        const phoneEl = document.getElementById('regPhone');
        const phone = phoneEl ? phoneEl.value.trim() : '';
        const password = document.getElementById('regPassword').value;
        const confirmPassword = document.getElementById('regConfirmPassword').value;
        const agreeTerms = document.getElementById('regAgreeTerms').checked;
        const submitBtn = document.getElementById('registerSubmitBtn');

        if (!fullName) {
            showAlert('registerAlert', 'Vui lòng nhập họ và tên của bạn.');
            document.getElementById('regFullName').focus();
            return;
        }
        if (!email || !isValidEmail(email)) {
            showAlert('registerAlert', 'Vui lòng nhập địa chỉ email hợp lệ.');
            document.getElementById('regEmail').focus();
            return;
        }
        if (!password || password.length < 6) {
            showAlert('registerAlert', 'Mật khẩu phải có độ dài tối thiểu 6 ký tự.');
            document.getElementById('regPassword').focus();
            return;
        }
        if (password !== confirmPassword) {
            showAlert('registerAlert', 'Mật khẩu xác nhận không trùng khớp.');
            document.getElementById('regConfirmPassword').focus();
            return;
        }
        if (!agreeTerms) {
            showAlert('registerAlert', 'Vui lòng đánh dấu đồng ý với Điều khoản dịch vụ.');
            return;
        }

        submitBtn.disabled = true;
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = `<span class="spinner-btn"></span> Đang tạo tài khoản...`;

        const body = new URLSearchParams({
            fullName: fullName,
            email: email,
            phone: phone,
            password: password,
            confirmPassword: confirmPassword
        });

        fetch('/Account/Register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            },
            body: (window.BloomCsrf ? window.BloomCsrf.withToken(body) : body).toString()
        })
        .then(res => res.json())
        .then(data => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            if (!data.success) {
                showAlert('registerAlert', data.message || 'Đăng ký không thành công.');
                return;
            }

            setUser(data.user);
            showAlert('registerAlert', 'Đăng ký thành công! Đang chuyển hướng...', true);

            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Tạo tài khoản thành công!',
                    text: `Chúc mừng ${data.user.name}! Bạn đã nhận được mã ưu đãi BLOOM10 giảm 10% cho đơn hàng đầu tiên.`,
                    confirmButtonText: 'Bắt Đầu Mua Sắm',
                    confirmButtonColor: '#0284C7'
                }).then(() => {
                    window.location.href = data.redirectUrl || '/';
                });
            } else {
                setTimeout(() => { window.location.href = '/'; }, 1200);
            }
        })
        .catch(() => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
            showAlert('registerAlert', 'Có lỗi kết nối máy chủ khi tạo tài khoản.');
        });
    }

    function handleForgotPassword(e) {
        e.preventDefault();
        hideAlert('forgotAlert');

        const email = document.getElementById('forgotEmail').value.trim();
        const submitBtn = document.getElementById('forgotSubmitBtn');

        if (!email || !isValidEmail(email)) {
            showAlert('forgotAlert', 'Vui lòng nhập địa chỉ email hợp lệ đã đăng ký.');
            document.getElementById('forgotEmail').focus();
            return;
        }
        submitBtn.disabled = true;
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = `<span class="spinner-btn"></span> Đang xử lý...`;

        const body = new URLSearchParams({ email: email });

        fetch('/Account/ForgotPassword', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            },
            body: (window.BloomCsrf ? window.BloomCsrf.withToken(body) : body).toString()
        })
        .then(res => res.json())
        .then(data => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            if (!data.success) {
                showAlert('forgotAlert', data.message || 'Không tìm thấy email này.');
                return;
            }

            showAlert('forgotAlert', data.message, true);
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Đã gửi email!',
                    text: data.message,
                    confirmButtonText: 'Đã hiểu',
                    confirmButtonColor: '#0284C7'
                });
            }
        })
        .catch(() => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
            showAlert('forgotAlert', 'Không thể kết nối máy chủ. Vui lòng thử lại.');
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        updateHeaderUser();
        syncFromServer();

        // Mobile drawer toggle
        const mobileToggleBtn = document.getElementById('mobileNavToggle');
        const mobileDrawer = document.getElementById('mobileNavDrawer');
        const mobileOverlay = document.getElementById('mobileNavOverlay');
        const mobileCloseBtn = document.getElementById('mobileNavClose');

        if (mobileToggleBtn && mobileDrawer && mobileOverlay) {
            mobileToggleBtn.addEventListener('click', () => {
                mobileDrawer.classList.add('active');
                mobileOverlay.classList.add('active');
            });
        }
        if (mobileCloseBtn && mobileDrawer && mobileOverlay) {
            mobileCloseBtn.addEventListener('click', () => {
                mobileDrawer.classList.remove('active');
                mobileOverlay.classList.remove('active');
            });
        }
        if (mobileOverlay && mobileDrawer) {
            mobileOverlay.addEventListener('click', () => {
                mobileDrawer.classList.remove('active');
                mobileOverlay.classList.remove('active');
            });
        }
    });

    return {
        getUser,
        setUser,
        logout,
        togglePassword,
        handleLogin,
        handleRegister,
        handleForgotPassword
    };
})();

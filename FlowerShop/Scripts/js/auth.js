/**
 * HỮU PHƯỚC FLORIST - CLIENT-SIDE AUTHENTICATION & USER STATE
 * Simulates Login, Register, Forgot Password flows, Form validation,
 * Loading/Error/Success states, and Header user profile synchronization.
 */

const BloomAuth = (function () {
    const USER_KEY = 'bloom_grace_user';

    function getUser() {
        try {
            const raw = localStorage.getItem(USER_KEY);
            return raw ? JSON.parse(raw) : null;
        } catch (e) {
            return null;
        }
    }

    function setUser(user) {
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        updateHeaderUser();
    }

    function logout() {
        localStorage.removeItem(USER_KEY);
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: 'Đã đăng xuất tài khoản!',
                showConfirmButton: false,
                timer: 1500
            }).then(() => {
                window.location.reload();
            });
        } else {
            window.location.reload();
        }
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
            userNameElements.forEach(el => el.textContent = user.name || 'Quý Khách');
            userEmailElements.forEach(el => el.textContent = user.email || '');
        } else {
            guestElements.forEach(el => el.style.display = 'block');
            userElements.forEach(el => el.style.display = 'none');
        }
    }

    // Toggle password visibility
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

    // Handle Login Form Submit
    function handleLogin(e) {
        e.preventDefault();
        hideAlert('loginAlert');

        const email = document.getElementById('loginEmail').value.trim();
        const password = document.getElementById('loginPassword').value;
        const submitBtn = document.getElementById('loginSubmitBtn');

        if (!email) {
            showAlert('loginAlert', 'Vui lòng nhập địa chỉ email của bạn.');
            document.getElementById('loginEmail').focus();
            return;
        }
        if (!isValidEmail(email)) {
            showAlert('loginAlert', 'Địa chỉ email không đúng định dạng (VD: example@domain.com).');
            document.getElementById('loginEmail').focus();
            return;
        }
        if (!password) {
            showAlert('loginAlert', 'Vui lòng nhập mật khẩu.');
            document.getElementById('loginPassword').focus();
            return;
        }
        if (password.length < 6) {
            showAlert('loginAlert', 'Mật khẩu phải chứa ít nhất 6 ký tự.');
            document.getElementById('loginPassword').focus();
            return;
        }

        // Simulate Loading State
        submitBtn.disabled = true;
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = `<span class="spinner-btn"></span> Đang xác thực...`;

        setTimeout(() => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            const name = email.split('@')[0];
            const capitalizedName = name.charAt(0).toUpperCase() + name.slice(1);
            setUser({
                name: capitalizedName,
                email: email
            });

            showAlert('loginAlert', 'Đăng nhập thành công! Đang chuyển hướng...', true);

            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Chào mừng trở lại!',
                    text: `Xin chào ${capitalizedName}, chúc bạn một ngày ngập tràn niềm vui!`,
                    showConfirmButton: false,
                    timer: 1600
                }).then(() => {
                    const redirectUrl = new URLSearchParams(window.location.search).get('returnUrl') || '/';
                    window.location.href = redirectUrl;
                });
            } else {
                setTimeout(() => { window.location.href = '/'; }, 1200);
            }
        }, 700);
    }

    // Handle Register Form Submit
    function handleRegister(e) {
        e.preventDefault();
        hideAlert('registerAlert');

        const fullName = document.getElementById('regFullName').value.trim();
        const email = document.getElementById('regEmail').value.trim();
        const password = document.getElementById('regPassword').value;
        const confirmPassword = document.getElementById('regConfirmPassword').value;
        const agreeTerms = document.getElementById('regAgreeTerms').checked;
        const submitBtn = document.getElementById('registerSubmitBtn');

        if (!fullName) {
            showAlert('registerAlert', 'Vui lòng nhập họ và tên của bạn.');
            document.getElementById('regFullName').focus();
            return;
        }
        if (!email) {
            showAlert('registerAlert', 'Vui lòng nhập địa chỉ email.');
            document.getElementById('regEmail').focus();
            return;
        }
        if (!isValidEmail(email)) {
            showAlert('registerAlert', 'Địa chỉ email không hợp lệ.');
            document.getElementById('regEmail').focus();
            return;
        }
        if (!password || password.length < 6) {
            showAlert('registerAlert', 'Mật khẩu phải có độ dài tối thiểu 6 ký tự.');
            document.getElementById('regPassword').focus();
            return;
        }
        if (password !== confirmPassword) {
            showAlert('registerAlert', 'Mật khẩu xác nhận không trùng khớp. Vui lòng kiểm tra lại.');
            document.getElementById('regConfirmPassword').focus();
            return;
        }
        if (!agreeTerms) {
            showAlert('registerAlert', 'Vui lòng đánh dấu đồng ý với Điều khoản dịch vụ.');
            return;
        }

        // Loading
        submitBtn.disabled = true;
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = `<span class="spinner-btn"></span> Đang tạo tài khoản...`;

        setTimeout(() => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            setUser({
                name: fullName,
                email: email
            });

            showAlert('registerAlert', 'Đăng ký thành công! Đang chuyển hướng...', true);

            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Tạo tài khoản thành công!',
                    text: `Chúc mừng ${fullName}! Bạn đã nhận được mã ưu đãi BLOOM10 giảm 10% cho đơn hàng đầu tiên.`,
                    confirmButtonText: 'Bắt Đầu Mua Sắm',
                    confirmButtonColor: '#0284C7'
                }).then(() => {
                    window.location.href = '/';
                });
            } else {
                setTimeout(() => { window.location.href = '/'; }, 1400);
            }
        }, 800);
    }

    // Handle Forgot Password Form Submit
    function handleForgotPassword(e) {
        e.preventDefault();
        hideAlert('forgotAlert');

        const email = document.getElementById('forgotEmail').value.trim();
        const submitBtn = document.getElementById('forgotSubmitBtn');

        if (!email) {
            showAlert('forgotAlert', 'Vui lòng nhập địa chỉ email liên kết với tài khoản.');
            document.getElementById('forgotEmail').focus();
            return;
        }
        if (!isValidEmail(email)) {
            showAlert('forgotAlert', 'Định dạng email không hợp lệ (VD: user@example.com).');
            document.getElementById('forgotEmail').focus();
            return;
        }

        // Loading
        submitBtn.disabled = true;
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = `<span class="spinner-btn"></span> Đang gửi hướng dẫn...`;

        setTimeout(() => {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            showAlert('forgotAlert', `Chúng tôi đã gửi hướng dẫn đặt lại mật khẩu đến ${email}. Vui lòng kiểm tra hộp thư đến hoặc thư rác!`, true);

            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'success',
                    title: 'Đã gửi liên kết khôi phục!',
                    text: `Hướng dẫn đổi mật khẩu đã được gửi đến email ${email}. Vui lòng kiểm tra hòm thư của bạn.`,
                    confirmButtonText: 'Quay Lại Đăng Nhập',
                    confirmButtonColor: '#0284C7'
                }).then(() => {
                    window.location.href = '/Account/Login';
                });
            }
        }, 750);
    }

    document.addEventListener('DOMContentLoaded', function () {
        updateHeaderUser();

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

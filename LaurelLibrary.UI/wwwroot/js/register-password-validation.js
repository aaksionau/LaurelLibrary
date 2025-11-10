/**
 * Password validation and visibility toggle functionality for the registration form
 */

document.addEventListener('DOMContentLoaded', function () {
    const passwordField = document.getElementById('passwordField');
    const togglePassword = document.getElementById('togglePassword');
    const togglePasswordIcon = document.getElementById('togglePasswordIcon');
    const registerForm = document.getElementById('registerForm');

    // Only execute if we're on the registration page with password field
    if (!passwordField || !togglePassword || !togglePasswordIcon || !registerForm) {
        return;
    }

    // Get password strength indicator
    const passwordStrength = document.getElementById('passwordStrength');
    let hasStartedTyping = false;

    // Password visibility toggle
    togglePassword.addEventListener('click', function () {
        const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordField.setAttribute('type', type);

        if (type === 'text') {
            togglePasswordIcon.classList.remove('bi-eye-slash');
            togglePasswordIcon.classList.add('bi-eye');
        } else {
            togglePasswordIcon.classList.remove('bi-eye');
            togglePasswordIcon.classList.add('bi-eye-slash');
        }
    });

    // Password validation
    function validatePassword(password) {
        const checks = {
            length: password.length >= 8,
            uppercase: /[A-Z]/.test(password),
            lowercase: /[a-z]/.test(password),
            digit: /\d/.test(password),
            special: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)
        };

        return checks;
    }

    function updatePasswordStrength(password) {
        const checks = validatePassword(password);

        // Update visual indicators
        Object.keys(checks).forEach(check => {
            const element = document.getElementById(check + '-check');
            if (!element) return;

            const icon = element.querySelector('i');
            if (!icon) return;

            if (checks[check]) {
                element.classList.remove('text-danger');
                element.classList.add('text-success');
                icon.classList.remove('bi-x-circle');
                icon.classList.add('bi-check-circle');
            } else {
                element.classList.remove('text-success');
                element.classList.add('text-danger');
                icon.classList.remove('bi-check-circle');
                icon.classList.add('bi-x-circle');
            }
        });

        return Object.values(checks).every(check => check);
    }

    // Real-time password validation
    passwordField.addEventListener('input', function () {
        const password = this.value;

        // Show password requirements when user starts typing
        if (password.length > 0 && !hasStartedTyping) {
            hasStartedTyping = true;
            if (passwordStrength) {
                passwordStrength.style.display = 'block';
            }
        }

        // Hide password requirements when field is empty
        if (password.length === 0 && hasStartedTyping) {
            hasStartedTyping = false;
            if (passwordStrength) {
                passwordStrength.style.display = 'none';
            }
            // Remove any existing error messages
            const existingError = document.getElementById('password-error');
            if (existingError) {
                existingError.remove();
            }
            return;
        }

        // Update password strength if we have content
        if (password.length > 0) {
            updatePasswordStrength(password);
        }
    });

    // Form submission validation
    registerForm.addEventListener('submit', function (e) {
        const password = passwordField.value;

        if (password.length === 0) {
            // Let server-side validation handle empty password
            return;
        }

        // Show password requirements if not already visible
        if (!hasStartedTyping && passwordStrength) {
            passwordStrength.style.display = 'block';
            hasStartedTyping = true;
        }

        const isValid = updatePasswordStrength(password);

        if (!isValid) {
            e.preventDefault();

            // Show error message
            const existingError = document.getElementById('password-error');
            if (existingError) {
                existingError.remove();
            }

            const errorDiv = document.createElement('div');
            errorDiv.id = 'password-error';
            errorDiv.className = 'alert alert-danger mt-2';
            errorDiv.innerHTML = '<i class="bi bi-exclamation-triangle me-2"></i>Please ensure your password meets all requirements.';

            passwordField.closest('.mb-3').appendChild(errorDiv);

            // Scroll to password field
            passwordField.focus();
            passwordField.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });
});
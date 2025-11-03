// Checkout page JavaScript functionality

// Initialize checkout page functionality
function initializeCheckoutPage(options) {
    const {
        hasCurrentReader = false
    } = options;

    if (hasCurrentReader) {
        // Focus on book input if reader is already selected
        const bookInput = document.getElementById('BookIsbn');
        if (bookInput) {
            bookInput.focus();
            // Keep focus on book input when it loses focus (for continuous scanning)
            bookInput.addEventListener('blur', function () {
                setTimeout(() => bookInput.focus(), 100);
            });
        }
    } else {
        // Focus on reader input if no reader selected
        const readerInput = document.getElementById('ReaderEan');
        if (readerInput) {
            readerInput.focus();
            // Keep focus on reader input when it loses focus (for continuous scanning)
            readerInput.addEventListener('blur', function () {
                setTimeout(() => readerInput.focus(), 100);
            });
        }
    }
}

// Auto-focus on the appropriate input field when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // This will be called from the Razor page with the appropriate options
});
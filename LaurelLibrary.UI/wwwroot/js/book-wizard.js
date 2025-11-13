/**
 * Book Wizard JavaScript functionality
 * Handles barcode scanning mode, ISBN input management, and form interactions
 */

// Constants
const SCANNING_MODE_KEY = 'bookWizardScanningMode';

// Initialize the page when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    initializeWizard();
});

/**
 * Initialize the book wizard functionality
 */
function initializeWizard() {
    const scanToggleBtn = document.getElementById('scanToggleBtn');
    const searchIsbnInput = document.getElementById('searchIsbnInput');
    const wizardForm = document.getElementById('wizardForm');
    const clearIsbnBtn = document.getElementById('clearIsbnBtn');

    // Clear the search ISBN input if it's empty (after successful search)
    if (searchIsbnInput && searchIsbnInput.value.trim() === '') {
        searchIsbnInput.value = '';
    }

    // Initialize scanning functionality
    initializeScanningMode(scanToggleBtn, searchIsbnInput);

    // Initialize input event handlers
    initializeInputHandlers(searchIsbnInput, wizardForm);

    // Initialize clear button
    initializeClearButton(clearIsbnBtn, searchIsbnInput);
}

/**
 * Initialize barcode scanning mode functionality
 */
function initializeScanningMode(scanToggleBtn, searchIsbnInput) {
    if (!scanToggleBtn) return;

    // Restore scanning mode from localStorage
    restoreScanningMode(scanToggleBtn, searchIsbnInput);

    // Add click handler for scan toggle button
    scanToggleBtn.addEventListener('click', function () {
        const isScanning = this.textContent.includes('Start');
        setScanningMode(isScanning, scanToggleBtn, searchIsbnInput);
    });
}

/**
 * Initialize input event handlers
 */
function initializeInputHandlers(searchIsbnInput, wizardForm) {
    if (!searchIsbnInput) return;

    // Keep focus on input while scanning
    searchIsbnInput.addEventListener('blur', function () {
        const scanBtn = document.getElementById('scanToggleBtn');
        if (scanBtn && scanBtn.textContent.includes('Stop')) {
            // Re-focus if we're in scanning mode
            setTimeout(() => {
                this.focus();
            }, 100);
        }
    });

    // Auto-submit form when Enter is pressed (barcode scanner sends Enter)
    searchIsbnInput.addEventListener('keypress', function (e) {
        const scanBtn = document.getElementById('scanToggleBtn');
        if (e.key === 'Enter' && scanBtn && scanBtn.textContent.includes('Stop')) {
            e.preventDefault();
            // Submit the form automatically
            if (wizardForm) {
                wizardForm.submit();
            }
        }
    });
}

/**
 * Initialize clear button functionality
 */
function initializeClearButton(clearIsbnBtn, searchIsbnInput) {
    if (!clearIsbnBtn || !searchIsbnInput) return;

    clearIsbnBtn.addEventListener('click', function () {
        searchIsbnInput.value = '';
        searchIsbnInput.focus();
    });
}

/**
 * Restore scanning mode from localStorage
 */
function restoreScanningMode(scanToggleBtn, searchIsbnInput) {
    const savedMode = localStorage.getItem(SCANNING_MODE_KEY);
    if (savedMode === 'true') {
        setScanningMode(true, scanToggleBtn, searchIsbnInput);
    }
}

/**
 * Set scanning mode and save to localStorage
 */
function setScanningMode(isScanning, scanToggleBtn, searchIsbnInput) {
    if (!scanToggleBtn || !searchIsbnInput) return;

    if (isScanning) {
        scanToggleBtn.innerHTML = '<i class="fas fa-barcode"></i> Stop Scanning';
        scanToggleBtn.classList.remove('btn-primary', 'scan-pulse');
        scanToggleBtn.classList.add('btn-warning');
        searchIsbnInput.classList.add('border-warning', 'border-2');
        searchIsbnInput.focus();
        localStorage.setItem(SCANNING_MODE_KEY, 'true');
    } else {
        scanToggleBtn.innerHTML = '<i class="fas fa-barcode"></i> Start Scanning';
        scanToggleBtn.classList.remove('btn-warning');
        scanToggleBtn.classList.add('btn-primary', 'scan-pulse');
        searchIsbnInput.classList.remove('border-warning', 'border-2');
        localStorage.setItem(SCANNING_MODE_KEY, 'false');
    }
}

/**
 * Print the barcode image directly without showing a popup
 */
function printBarcode() {
    var barcodeImage = document.getElementById('barcodeImage');

    if (barcodeImage) {
        // Create a new window for printing
        var printWindow = window.open('', '_blank');

        printWindow.document.write('<html><head><title>Print Barcode</title>');
        printWindow.document.write('<style>');
        printWindow.document.write('@page { size: auto; margin: 10mm; }');
        printWindow.document.write('body { margin: 0; text-align: center; }');
        printWindow.document.write('img { max-width: 100%; height: auto; }');
        printWindow.document.write('</style>');
        printWindow.document.write('</head><body>');
        printWindow.document.write('<img src="' + barcodeImage.src + '" alt="Barcode" />');
        printWindow.document.write('</body></html>');
        printWindow.document.close();

        // Wait for image to load before printing
        setTimeout(function () {
            printWindow.print();
            printWindow.close();
        }, 250);
    }
}

/**
 * Print the barcode image from the modal
 */
function printBarcodeImage() {
    var modalImage = document.getElementById('modalBarcodeImage');

    if (modalImage) {
        // Create a new window for printing
        var printWindow = window.open('', '_blank');

        printWindow.document.write('<html><head><title>Print Barcode</title>');
        printWindow.document.write('<style>');
        printWindow.document.write('@page { size: auto; margin: 10mm; }');
        printWindow.document.write('body { margin: 0; text-align: center; }');
        printWindow.document.write('img { max-width: 100%; height: auto; }');
        printWindow.document.write('</style>');
        printWindow.document.write('</head><body>');
        printWindow.document.write('<img src="' + modalImage.src + '" alt="Barcode" />');
        printWindow.document.write('</body></html>');
        printWindow.document.close();

        // Wait for image to load before printing
        setTimeout(function () {
            printWindow.print();
            printWindow.close();
        }, 250);
    }
}

// Export functions for global access if needed
window.setScanningMode = setScanningMode;
window.restoreScanningMode = restoreScanningMode;
window.printBarcode = printBarcode;
window.printBarcodeImage = printBarcodeImage;
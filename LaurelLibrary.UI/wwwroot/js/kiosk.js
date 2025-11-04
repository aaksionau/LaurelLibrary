// Kiosk page JavaScript functionality

// Success overlay functionality
function closeSuccessOverlay() {
    const overlay = document.getElementById('successOverlay');
    overlay.style.display = 'none';
}

function closeReturnSuccessOverlay() {
    const overlay = document.getElementById('returnSuccessOverlay');
    overlay.style.display = 'none';
}

// Initialize kiosk page functionality
function initializeKioskPage(options) {
    const { showCheckoutSuccess = false, showReturnSuccess = false } = options;

    // Show success overlay if checkout was successful
    if (showCheckoutSuccess) {
        document.addEventListener('DOMContentLoaded', function () {
            const overlay = document.getElementById('successOverlay');
            overlay.style.display = 'flex';

            // Auto-close after 5 seconds
            setTimeout(function () {
                closeSuccessOverlay();
            }, 5000);
        });
    }

    // Show return success overlay if return was successful
    if (showReturnSuccess) {
        document.addEventListener('DOMContentLoaded', function () {
            const overlay = document.getElementById('returnSuccessOverlay');
            overlay.style.display = 'flex';

            // Auto-close after 5 seconds
            setTimeout(function () {
                closeReturnSuccessOverlay();
            }, 5000);
        });
    }
}

// Check for kiosk parameters and redirect if available (for Index page)
async function checkAndRedirectToKiosk() {
    // Get or generate browserFingerprint
    let browserFingerprint = localStorage.getItem('browserFingerprint');

    if (!browserFingerprint) {
        try {
            // Initialize FingerprintJS
            const FingerprintJS = await import('https://openfpcdn.io/fingerprintjs/v4');
            const fp = await FingerprintJS.load();
            const result = await fp.get();
            browserFingerprint = result.visitorId;

            // Save to localStorage
            localStorage.setItem('browserFingerprint', browserFingerprint);
        } catch (error) {
            console.error('Error generating fingerprint:', error);
            // Fallback to a random ID if FingerprintJS fails
            browserFingerprint = 'fallback-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('browserFingerprint', browserFingerprint);
        }
    }

    // Get other parameters from localStorage
    const libraryId = localStorage.getItem('libraryId');
    const kioskId = localStorage.getItem('kioskId');

    // If we have all required parameters in localStorage, redirect to kiosk
    if (libraryId && kioskId && browserFingerprint) {
        const params = new URLSearchParams();
        params.set('libraryId', libraryId);
        params.set('kioskId', kioskId);
        params.set('browserFingerprint', browserFingerprint);

        // Redirect to kiosk page with parameters
        window.location.href = `/Kiosk?${params.toString()}`;
    }
}

// Smooth scrolling for anchor links
function initializeSmoothScrolling() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

// Add scroll effect to hero section
function initializeParallaxEffect() {
    window.addEventListener('scroll', function () {
        const scrolled = window.pageYOffset;
        const parallax = document.querySelector('.hero-section');
        if (parallax) {
            const speed = scrolled * 0.5;
            parallax.style.transform = `translateY(${speed}px)`;
        }
    });
}
document.addEventListener('DOMContentLoaded', function () {
    const selectAllCheckbox = document.getElementById('selectAll');
    const personCheckboxes = document.querySelectorAll('.person-checkbox');
    const selectedCountSpan = document.getElementById('selectedCount');
    const importBtn = document.getElementById('importBtn');

    function updateSelectedCount() {
        const selectedCount = document.querySelectorAll('.person-checkbox:checked').length;
        selectedCountSpan.textContent = selectedCount;
        importBtn.disabled = selectedCount === 0;
    }

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            personCheckboxes.forEach(checkbox => {
                checkbox.checked = this.checked;
            });
            updateSelectedCount();
        });
    }

    personCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            updateSelectedCount();

            // Update select all checkbox state
            if (selectAllCheckbox) {
                const allChecked = Array.from(personCheckboxes).every(cb => cb.checked);
                const noneChecked = Array.from(personCheckboxes).every(cb => !cb.checked);

                selectAllCheckbox.checked = allChecked;
                selectAllCheckbox.indeterminate = !allChecked && !noneChecked;
            }
        });
    });

    // Initialize count on page load
    updateSelectedCount();
});
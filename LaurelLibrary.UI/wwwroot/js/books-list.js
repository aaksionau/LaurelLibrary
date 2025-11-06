// Books List Page JavaScript
$(document).ready(function () {
    // Initialize Select2 dropdowns with AJAX autocomplete
    $('#authorSelect').select2({
        theme: 'bootstrap-5',
        placeholder: 'All Authors',
        allowClear: true,
        width: '100%',
        ajax: {
            url: '/api/authors/search',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return {
                    q: params.term, // search term
                    limit: 20
                };
            },
            processResults: function (data) {
                // Transform the response to Select2 format
                return {
                    results: data.map(function (author) {
                        return {
                            id: author.authorId,
                            text: author.fullName
                        };
                    })
                };
            },
            cache: true
        },
        minimumInputLength: 2, // Require at least 2 characters before searching
        templateResult: function (author) {
            if (author.loading) {
                return author.text;
            }
            return $('<span>' + author.text + '</span>');
        },
        templateSelection: function (author) {
            return author.text || author.fullName;
        }
    });

    $('#categorySelect').select2({
        theme: 'bootstrap-5',
        placeholder: 'All Categories',
        allowClear: true,
        width: '100%',
        ajax: {
            url: '/api/categories/search',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return {
                    q: params.term, // search term
                    limit: 20
                };
            },
            processResults: function (data) {
                // Transform the response to Select2 format
                return {
                    results: data.map(function (category) {
                        return {
                            id: category.categoryId,
                            text: category.name
                        };
                    })
                };
            },
            cache: true
        },
        minimumInputLength: 2, // Require at least 2 characters before searching
        templateResult: function (category) {
            if (category.loading) {
                return category.text;
            }
            return $('<span>' + category.text + '</span>');
        },
        templateSelection: function (category) {
            return category.text || category.name;
        }
    });

    // Multiple selection functionality
    function updateSelectedCount() {
        var selectedCount = $('.book-checkbox:checked').length;

        if (selectedCount > 0) {
            $('#selectedCount').text(selectedCount + ' selected').show();
        } else {
            $('#selectedCount').hide();
        }

        $('#deleteSelectedBtn').prop('disabled', selectedCount === 0);
    }

    // Handle individual checkbox changes
    $('.book-checkbox').change(function () {
        updateSelectedCount();

        // Update the "select all" checkbox state
        var totalCheckboxes = $('.book-checkbox').length;
        var checkedCheckboxes = $('.book-checkbox:checked').length;

        if (checkedCheckboxes === 0) {
            $('#selectAllCheckbox').prop('indeterminate', false).prop('checked', false);
        } else if (checkedCheckboxes === totalCheckboxes) {
            $('#selectAllCheckbox').prop('indeterminate', false).prop('checked', true);
        } else {
            $('#selectAllCheckbox').prop('indeterminate', true);
        }
    });

    // Handle "select all" checkbox
    $('#selectAllCheckbox').change(function () {
        var isChecked = $(this).prop('checked');
        $('.book-checkbox').prop('checked', isChecked);
        updateSelectedCount();
    });

    // Initialize count
    updateSelectedCount();
});

// Function to handle delete multiple books
function deleteMultipleBooks() {
    console.log('Delete multiple button clicked');

    var selectedCheckboxes = $('.book-checkbox:checked');
    var selectedCount = selectedCheckboxes.length;
    console.log('Selected count:', selectedCount);

    if (selectedCount === 0) {
        alert('Please select at least one book to delete.');
        return false;
    }

    var confirmed = confirm(`Are you sure you want to delete ${selectedCount} selected book(s)?`);
    if (!confirmed) {
        console.log('User cancelled deletion');
        return false;
    }

    console.log('User confirmed deletion, creating form');

    // Create a form dynamically
    var form = document.createElement('form');
    form.method = 'POST';
    form.action = window.location.pathname + '?handler=DeleteMultiple';

    // Add anti-forgery token
    var token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        var tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = token;
        form.appendChild(tokenInput);
    }

    // Add selected book IDs
    selectedCheckboxes.each(function () {
        var input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'SelectedBookIds';
        input.value = $(this).val();
        form.appendChild(input);
        console.log('Added book ID:', $(this).val());
    });

    // Add page number and page size if they exist
    var pageNumberInput = $('input[name="pageNumber"]');
    if (pageNumberInput.length > 0) {
        var pageInput = document.createElement('input');
        pageInput.type = 'hidden';
        pageInput.name = 'pageNumber';
        pageInput.value = pageNumberInput.val();
        form.appendChild(pageInput);
    }

    var pageSizeInput = $('input[name="pageSize"]');
    if (pageSizeInput.length > 0) {
        var sizeInput = document.createElement('input');
        sizeInput.type = 'hidden';
        sizeInput.name = 'pageSize';
        sizeInput.value = pageSizeInput.val();
        form.appendChild(sizeInput);
    }

    // Add form to page and submit
    document.body.appendChild(form);
    console.log('Submitting form to:', form.action);
    form.submit();
}

// Toggle between traditional and semantic search
function toggleSearchMode() {
    var useSemanticSearch = document.getElementById('semanticSearchToggle').checked;
    var semanticSection = document.getElementById('semanticSearchSection');
    var traditionalSection = document.getElementById('traditionalSearchSection');

    if (useSemanticSearch) {
        semanticSection.style.display = 'block';
        traditionalSection.style.display = 'none';
        // Clear traditional search fields when switching to semantic
        document.getElementById('titleSearch').value = '';
        document.getElementById('authorSelect').value = '';
        document.getElementById('categorySelect').value = '';
    } else {
        semanticSection.style.display = 'none';
        traditionalSection.style.display = 'block';
        // Clear semantic search field when switching to traditional
        document.getElementById('semanticSearchInput').value = '';
    }
}

// Handle form submission for semantic search loading indicator
document.addEventListener('DOMContentLoaded', function () {
    document.querySelector('form').addEventListener('submit', function (e) {
        var useSemanticSearch = document.getElementById('semanticSearchToggle').checked;
        var semanticQuery = document.getElementById('semanticSearchInput').value.trim();

        if (useSemanticSearch && semanticQuery) {
            // Show loading state for semantic search
            var searchButton = document.getElementById('searchButton');
            var searchIcon = document.getElementById('searchIcon');
            var searchSpinner = document.getElementById('searchSpinner');
            var searchButtonText = document.getElementById('searchButtonText');

            searchButton.disabled = true;
            searchIcon.style.display = 'none';
            searchSpinner.style.display = 'inline-block';
            searchButtonText.textContent = 'AI Processing...';
        }
    });
});
var activeSelectElement = null; // Stores the select element that triggered the modal

// Global JS object to manage Quick Add
const QuickAddManager = {
    // Configs for each master type
    configs: {
        'Manufacturer': {
            title: 'Manufacturer',
            label: 'Manufacturer Name',
            url: '/Manufacturer/QuickAdd',
            showPhone: true,
            showEmail: false,
            showDesc: false
        },
        'Supplier': {
            title: 'Supplier',
            label: 'Supplier Name',
            url: '/Suppliers/QuickAdd',
            showPhone: true,
            showEmail: true,
            showDesc: false
        },
        'GenericMedicine': {
            title: 'Generic Medicine',
            label: 'Generic Name',
            url: '/GenericMedicine/QuickAdd',
            showPhone: false,
            showEmail: false,
            showDesc: true
        },
        'MedicineCategory': {
            title: 'Medicine Category',
            label: 'Category Name',
            url: '/MedicineCategory/QuickAdd',
            showPhone: false,
            showEmail: false,
            showDesc: true
        },
        'MedicineUnit': {
            title: 'Medicine Unit',
            label: 'Unit Name',
            url: '/MedicineUnit/QuickAdd',
            showPhone: false,
            showEmail: false,
            showDesc: false
        }
    },

    // Initializer function for Select2 fields
    init: function (selector, type) {
        const config = this.configs[type];
        if (!config) return;

        const $element = $(selector);
        
        // Re-initialize select2 with Quick Add capabilities
        $element.select2({
            theme: 'bootstrap-5',
            width: '100%',
            dropdownParent: $element.closest('.modal').length ? $element.closest('.modal') : $(document.body),
            language: {
                noResults: function (params) {
                    if (params && params.term) {
                        return `<button type="button" class="btn btn-sm btn-link text-primary w-100 text-start p-1 border-0 quick-add-btn" 
                                    data-type="${type}" 
                                    data-term="${escapeHtml(params.term)}">
                                    <i class="fas fa-plus-circle me-1"></i> Add New "${escapeHtml(params.term)}"
                                </button>`;
                    }
                    return "No results found";
                }
            },
            escapeMarkup: function (markup) {
                return markup;
            }
        });
    },

    // Open Modal
    openModal: function (type, term) {
        const config = this.configs[type];
        if (!config) return;

        // Reset form
        $('#quickAddForm')[0].reset();
        $('#quickAddErrorBox').addClass('d-none');
        $('#valQuickAddName').addClass('d-none');

        // Set type
        $('#quickAddType').val(type);

        // Configure Modal UI
        $('#quickAddTitle').text('Add New ' + config.title);
        $('#lblQuickAddName').text(config.label);
        $('#quickAddName').val(term).attr('placeholder', 'Enter ' + config.label.toLowerCase());

        // Toggle Dynamic Fields
        if (config.showDesc) {
            $('#divQuickAddDescription').removeClass('d-none');
        } else {
            $('#divQuickAddDescription').addClass('d-none');
        }

        if (config.showPhone) {
            $('#divQuickAddPhone').removeClass('d-none');
        } else {
            $('#divQuickAddPhone').addClass('d-none');
        }

        if (config.showEmail) {
            $('#divQuickAddEmail').removeClass('d-none');
        } else {
            $('#divQuickAddEmail').addClass('d-none');
        }

        // Show Modal
        const modal = new bootstrap.Modal(document.getElementById('quickAddModal'));
        modal.show();

        // Focus Name field
        setTimeout(() => {
            $('#quickAddName').focus().select();
        }, 500);
    }
};

// Intercept clicks on Add New button in Select2 dropdowns
$(document).on('click', '.quick-add-btn', function (e) {
    e.preventDefault();
    e.stopPropagation();

    const type = $(this).data('type');
    const term = $(this).data('term');

    // Identify which select element triggered the popup
    const $select2Container = $(this).closest('.select2-container');
    if ($select2Container.length) {
        activeSelectElement = $('.select2-hidden-accessible').filter(function () {
            return $(this).data('select2') && $(this).data('select2').$container[0] === $select2Container.prev('.select2-container')[0] || 
                   $(this).next('.select2-container')[0] === $select2Container[0];
        });
    }

    // Close select2 dropdown
    if (activeSelectElement) {
        $(activeSelectElement).select2('close');
    }

    // Open Modal
    QuickAddManager.openModal(type, term);
});

// Save Function
function saveQuickAdd() {
    const type = $('#quickAddType').val();
    const config = QuickAddManager.configs[type];
    if (!config) return;

    const name = $('#quickAddName').val().trim();
    if (!name) {
        $('#valQuickAddName').removeClass('d-none');
        $('#quickAddName').focus();
        return;
    }
    $('#valQuickAddName').addClass('d-none');
    $('#quickAddErrorBox').addClass('d-none');

    // Prep dynamic payload
    const payload = {
        Name: name
    };

    if (config.showDesc) {
        payload.Description = $('#quickAddDescription').val().trim();
    }
    if (config.showPhone) {
        payload.Phone = $('#quickAddPhone').val().trim();
    }
    if (config.showEmail) {
        payload.Email = $('#quickAddEmail').val().trim();
    }

    // Show loading spinner
    $('#spinnerQuickAdd').removeClass('d-none');
    $('#iconQuickAddSave').addClass('d-none');
    $('#btnQuickAddSave').attr('disabled', true);

    $.ajax({
        url: config.url,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (res) {
            // Hide loading spinner
            $('#spinnerQuickAdd').addClass('d-none');
            $('#iconQuickAddSave').removeClass('d-none');
            $('#btnQuickAddSave').removeAttr('disabled');

            if (res.success) {
                toastr.success(res.message || 'Record saved successfully.');
                
                // Hide modal
                const modalEl = document.getElementById('quickAddModal');
                const modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) modal.hide();

                // Auto-append and select in dropdown
                if (activeSelectElement) {
                    const newOption = new Option(res.text, res.id, true, true);
                    $(activeSelectElement).append(newOption).trigger('change');
                    
                    // Trigger custom select2 focus logic if available
                    $(activeSelectElement).trigger({
                        type: 'select2:select',
                        params: {
                            data: {
                                id: res.id,
                                text: res.text
                            }
                        }
                    });
                }
            } else {
                $('#quickAddErrorText').text(res.message || 'Failed to save record.');
                $('#quickAddErrorBox').removeClass('d-none');
            }
        },
        error: function () {
            $('#spinnerQuickAdd').addClass('d-none');
            $('#iconQuickAddSave').removeClass('d-none');
            $('#btnQuickAddSave').removeAttr('disabled');
            toastr.error('Network error. Failed to save record.');
        }
    });
}

// Helper to escape HTML to prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    return text
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

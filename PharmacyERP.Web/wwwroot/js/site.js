// Common notification functions
const notify = {
    success: function (message) {
        toastr.success(message);
    },
    error: function (message) {
        toastr.error(message || "Something went wrong!");
    },
    info: function (message) {
        toastr.info(message);
    },
    warning: function (message) {
        toastr.warning(message);
    }
};

// Common confirmation dialog
const confirmAction = (title, text, callback) => {
    Swal.fire({
        title: title || 'Are you sure?',
        text: text || "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, proceed!'
    }).then((result) => {
        if (result.isConfirmed) {
            callback();
        }
    });
};

// Global AJAX error handling
$(document).ajaxError(function (event, jqxhr, settings, thrownError) {
    if (jqxhr.status === 401) {
        notify.error("Session expired. Please login again.");
        window.location.href = "/Account/Login";
    } else if (jqxhr.status === 403) {
        notify.error("You don't have permission to perform this action.");
    } else {
        notify.error("An error occurred during the request.");
    }
});

// Global Loading Overlay Controls
const showGlobalLoader = (message) => {
    if (message) {
        $('#global-loader .loader-text').text(message);
    } else {
        $('#global-loader .loader-text').text("Please wait, processing your request...");
    }
    $('#global-loader').fadeIn(150);
};

const hideGlobalLoader = () => {
    $('#global-loader').fadeOut(150);
};

// Global AJAX Request tracking with ignore rules for autocomplete and custom workflows
let activeAjaxCount = 0;

$(document).ajaxSend(function (event, jqXHR, settings) {
    const url = settings.url.toLowerCase();
    
    // Ignore background, type-ahead search, and pre-confirmed save operations
    if (url.includes('searchmedicine') || 
        url.includes('searchcustomers') || 
        url.includes('getprescriptions') ||
        url.includes('/sales/pos') ||
        url.includes('/purchases/create')) {
        return;
    }

    activeAjaxCount++;
    if (activeAjaxCount === 1) {
        showGlobalLoader();
    }
});

$(document).ajaxComplete(function (event, jqXHR, settings) {
    const url = settings.url.toLowerCase();
    
    if (url.includes('searchmedicine') || 
        url.includes('searchcustomers') || 
        url.includes('getprescriptions') ||
        url.includes('/sales/pos') ||
        url.includes('/purchases/create')) {
        return;
    }

    activeAjaxCount--;
    if (activeAjaxCount <= 0) {
        activeAjaxCount = 0;
        hideGlobalLoader();
    }
});

// Hook into non-AJAX traditional Form submits & Report generation links
$(document).ready(function () {
    // Intercept normal HTML forms (Settings updates, Master additions, etc.)
    $('form').not('[data-ajax="true"]').on('submit', function () {
        // Skip user logout click
        if ($(this).attr('action') && $(this).attr('action').toLowerCase().includes('logout')) {
            return;
        }
        showGlobalLoader("Processing your request, please wait...");
    });

    // Intercept Report downloads or compilation links (excluding blank targets)
    $('a').on('click', function () {
        const href = $(this).attr('href');
        if (href && (href.toLowerCase().includes('/reports/') || href.toLowerCase().includes('export'))) {
            if ($(this).attr('target') === '_blank') {
                return;
            }
            showGlobalLoader("Generating report, please wait...");
        }
    });
});

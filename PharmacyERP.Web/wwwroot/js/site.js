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

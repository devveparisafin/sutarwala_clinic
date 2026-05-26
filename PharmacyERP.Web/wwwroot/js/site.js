// =============================================================================
// COMMON UTILITIES
// =============================================================================

const notify = {
    success: function (message) { toastr.success(message); },
    error:   function (message) { toastr.error(message || "Something went wrong!"); },
    info:    function (message) { toastr.info(message); },
    warning: function (message) { toastr.warning(message); }
};

const confirmAction = (title, text, callback) => {
    Swal.fire({
        title: title || 'Are you sure?',
        text:  text  || "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor:  '#d33',
        confirmButtonText:  'Yes, proceed!'
    }).then((result) => { if (result.isConfirmed) callback(); });
};

// Global Loading Overlay Controls
const showGlobalLoader = (message) => {
    $('#global-loader .loader-text').text(message || "Please wait, processing your request...");
    $('#global-loader').fadeIn(150);
};

const hideGlobalLoader = () => {
    $('#global-loader').fadeOut(150);
};

// =============================================================================
// GLOBAL AJAX ERROR HANDLING & LOADER
// =============================================================================

$(document).ajaxError(function (event, jqxhr) {
    if (jqxhr.status === 401) {
        notify.error("Session expired. Please login again.");
        window.location.href = "/Account/Login";
    } else if (jqxhr.status === 403) {
        notify.error("You don't have permission to perform this action.");
    }
    // Note: general errors handled per-request
});

let activeAjaxCount = 0;

const _ajaxIgnoreUrls = ['searchmedicine', 'searchcustomers', 'getprescriptions',
                         '/sales/pos', '/purchases/create', 'getlowstockalerts',
                         'ping', 'favicon'];

function _isIgnoredAjaxUrl(url) {
    const u = url.toLowerCase();
    return _ajaxIgnoreUrls.some(p => u.includes(p));
}

$(document).ajaxSend(function (e, jqXHR, settings) {
    if (_isIgnoredAjaxUrl(settings.url)) return;
    if (++activeAjaxCount === 1) showGlobalLoader();
});

$(document).ajaxComplete(function (e, jqXHR, settings) {
    if (_isIgnoredAjaxUrl(settings.url)) return;
    if (--activeAjaxCount <= 0) { activeAjaxCount = 0; hideGlobalLoader(); }
});

$(document).ready(function () {
    // Intercept standard HTML form submits (show loader)
    $('form').not('[data-ajax="true"]').on('submit', function () {
        if ($(this).attr('action') && $(this).attr('action').toLowerCase().includes('logout')) return;
        showGlobalLoader("Processing your request, please wait...");
    });

    // Show loader on report/export link clicks
    $('a').on('click', function () {
        const href = $(this).attr('href');
        if (href && (href.toLowerCase().includes('/reports/') || href.toLowerCase().includes('export'))) {
            if ($(this).attr('target') !== '_blank') showGlobalLoader("Generating report, please wait...");
        }
    });
});

// =============================================================================
// FEATURE 1: REAL CONNECTIVITY INDICATOR (server ping — not navigator.onLine)
// navigator.onLine is unreliable: shows "online" even when WiFi is disconnected.
// We do an actual lightweight fetch to the server every 15 seconds instead.
// =============================================================================

let _isCurrentlyOnline = true;     // assumed online at start
let _connectivityTimer = null;
let _offlineToastShown = false;

function _updateConnectivityUI(isOnline) {
    const dot    = document.getElementById('connectivityDot');
    const text   = document.getElementById('connectivityText');
    const banner = document.getElementById('offlineBanner');
    if (!dot || !text) return;

    const wasOnline = _isCurrentlyOnline;
    _isCurrentlyOnline = isOnline;

    if (isOnline) {
        dot.style.background  = '#10b981';
        dot.style.boxShadow   = '0 0 6px #10b981';
        text.textContent      = 'Online';
        text.style.color      = '#065f46';
        if (banner) banner.style.display = 'none';
        $('button[type="submit"], input[type="submit"]').removeAttr('disabled');

        // Toast only when recovering from offline
        if (!wasOnline) {
            _offlineToastShown = false;
            toastr.success("Connection restored!", "Back Online", { timeOut: 3000 });
        }
    } else {
        dot.style.background  = '#ef4444';
        dot.style.boxShadow   = '0 0 6px #ef4444';
        text.textContent      = 'Offline';
        text.style.color      = '#991b1b';

        if (banner) {
            banner.style.display = 'block';
            // Auto-hide banner after 8 seconds
            clearTimeout(banner._hideTimer);
            banner._hideTimer = setTimeout(() => { banner.style.display = 'none'; }, 8000);
        }

        $('button[type="submit"], input[type="submit"]').attr('disabled', 'disabled');

        if (!_offlineToastShown) {
            _offlineToastShown = true;
            toastr.warning("No internet connection. Please check your network.", "Offline", { timeOut: 5000 });
        }
    }
}

async function _checkConnectivity() {
    try {
        // Fetch a tiny endpoint on OUR server with a cache-busting param
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 4000); // 4s timeout
        const res = await fetch('/favicon.ico?_=' + Date.now(), {
            method: 'HEAD',
            cache: 'no-store',
            signal: controller.signal
        });
        clearTimeout(timeout);
        _updateConnectivityUI(res.ok || res.status < 500);
    } catch (e) {
        // fetch threw — network is truly unreachable
        _updateConnectivityUI(false);
    }
}

// Start connectivity polling on page load
$(document).ready(function () {
    // First check after 1 second (let page settle)
    setTimeout(_checkConnectivity, 1000);
    // Then poll every 15 seconds
    _connectivityTimer = setInterval(_checkConnectivity, 15000);

    // Also use browser events as a fast trigger, then verify with real ping
    window.addEventListener('online',  () => setTimeout(_checkConnectivity, 500));
    window.addEventListener('offline', () => _updateConnectivityUI(false));
});

// =============================================================================
// FEATURE 2: FORM AUTO-DRAFT (localStorage)
// =============================================================================
(function () {
    const DRAFT_PREFIX      = 'erpDraft_';
    const DRAFT_INTERVAL_MS = 30000;
    const currentPage       = window.location.pathname;
    const draftKey          = DRAFT_PREFIX + currentPage;

    function serializeForm(form) {
        const data = {};
        $(form).find('input:not([type=password]):not([type=file]):not([type=submit]):not([type=button]), textarea, select')
               .each(function () {
                   const name = $(this).attr('name') || $(this).attr('id');
                   if (name && $(this).val()) data[name] = $(this).val();
               });
        return data;
    }

    function saveDraft() {
        const forms = $('form').not('[data-no-draft]');
        if (!forms.length) return;
        const allData = {};
        forms.each(function (i, form) {
            const fd = serializeForm(form);
            if (Object.keys(fd).length) allData['form_' + i] = fd;
        });
        if (Object.keys(allData).length) {
            try { localStorage.setItem(draftKey, JSON.stringify({ savedAt: new Date().toISOString(), data: allData })); }
            catch (e) { /* storage full */ }
        }
    }

    function clearDraft() { localStorage.removeItem(draftKey); }

    function restoreDraft(savedDraft) {
        const forms = $('form').not('[data-no-draft]');
        try {
            forms.each(function (i, form) {
                const fd = savedDraft.data['form_' + i];
                if (!fd) return;
                Object.keys(fd).forEach(key => {
                    const $el = $(form).find(`[name="${key}"], #${key}`).first();
                    if ($el.length && !$el.val()) $el.val(fd[key]).trigger('change');
                });
            });
        } catch (e) { /* ignore */ }
    }

    $(document).ready(function () {
        const isAccountPage = currentPage.includes('/Account/') || currentPage === '/';
        if (isAccountPage) return;
        if (!$('form').not('[data-no-draft]').length) return;

        try {
            const savedRaw = localStorage.getItem(draftKey);
            if (savedRaw) {
                const saved      = JSON.parse(savedRaw);
                const ageMinutes = (Date.now() - new Date(saved.savedAt).getTime()) / 60000;
                if (ageMinutes < 120) {
                    const timeAgo = ageMinutes < 1 ? 'just now' : Math.round(ageMinutes) + ' min ago';
                    toastr.info(
                        `<div><strong>Unsaved draft found</strong> (saved ${timeAgo})<br>
                         <button class="btn btn-sm btn-light mt-1 me-1" onclick="window._restoreDraftFn()">Restore</button>
                         <button class="btn btn-sm btn-outline-secondary mt-1" onclick="window._clearDraftFn()">Discard</button></div>`,
                        '', { timeOut: 12000, extendedTimeOut: 5000, enableHtml: true, closeButton: true }
                    );
                    window._restoreDraftFn = () => { restoreDraft(saved); clearDraft(); toastr.clear(); toastr.success("Draft restored!"); };
                    window._clearDraftFn  = () => { clearDraft(); toastr.clear(); };
                } else {
                    clearDraft();
                }
            }
        } catch (e) { /* ignore */ }

        setInterval(saveDraft, DRAFT_INTERVAL_MS);
        document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'hidden') saveDraft(); });
        $('form').not('[data-no-draft]').on('submit', clearDraft);
    });
})();

// =============================================================================
// FEATURE 3: LOW STOCK NOTIFICATION PANEL
// =============================================================================

let _lowStockPanelOpen   = false;
let _lowStockPanelLoaded = false;

function openLowStockPanel() {
    const overlay = document.getElementById('lowStockOverlay');
    const panel   = document.getElementById('lowStockPanel');
    if (!overlay || !panel) return;

    overlay.style.display = 'block';
    panel.style.right     = '0';
    _lowStockPanelOpen    = true;

    if (!_lowStockPanelLoaded) {
        loadLowStockData();
    }
}

function closeLowStockPanel() {
    const overlay = document.getElementById('lowStockOverlay');
    const panel   = document.getElementById('lowStockPanel');
    if (!overlay || !panel) return;

    overlay.style.display = 'none';
    panel.style.right     = '-420px';
    _lowStockPanelOpen    = false;
}

// Close panel with Escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && _lowStockPanelOpen) closeLowStockPanel();
});

function _updateLowStockBadge(count) {
    const badge = document.getElementById('lowStockBadge');
    if (!badge) return;
    if (count > 0) {
        badge.textContent = count > 99 ? '99+' : count;
        badge.classList.remove('d-none');
    } else {
        badge.classList.add('d-none');
    }
}

function loadLowStockData() {
    const list     = document.getElementById('lowStockList');
    const subtitle = document.getElementById('lowStockSubtitle');
    if (!list) return;

    // Show spinner
    list.innerHTML = `<div class="text-center py-5 text-muted">
        <div class="spinner-border spinner-border-sm text-primary mb-2" role="status"></div>
        <div class="small">Loading alerts...</div>
    </div>`;

    $.ajax({
        url: '/Inventory/GetLowStockAlerts',
        type: 'GET',
        dataType: 'json',
        success: function (res) {
            _lowStockPanelLoaded = true;

            if (!res || !res.success) {
                list.innerHTML = '<div class="text-center text-danger py-4"><i class="fas fa-exclamation-triangle fa-2x mb-2 d-block"></i>Failed to load alerts.</div>';
                return;
            }

            _updateLowStockBadge(res.count);
            if (subtitle) subtitle.textContent = res.count > 0 ? res.count + ' medicine(s) need attention' : 'All stock levels are healthy';

            if (!res.items || res.items.length === 0) {
                list.innerHTML = `<div class="text-center py-5">
                    <div style="font-size:3rem;">✅</div>
                    <h6 class="fw-bold text-success mt-2">All Good!</h6>
                    <p class="text-muted small">No medicines are below their minimum stock threshold.</p>
                </div>`;
                return;
            }

            let html = '';
            res.items.forEach(function (item) {
                const stockPct     = item.threshold > 0 ? Math.round((item.currentStock / item.threshold) * 100) : 0;
                const urgencyColor = item.currentStock === 0 ? '#dc2626'
                                   : item.currentStock <= Math.floor(item.threshold / 2) ? '#ea580c'
                                   : '#d97706';
                const urgencyLabel = item.currentStock === 0 ? 'Out of Stock' : 'Low Stock';
                const barWidth     = Math.min(stockPct, 100);

                html += `<div style="background:#fff;border:1px solid #e2e8f0;border-left:4px solid ${urgencyColor};border-radius:8px;padding:0.85rem 1rem;margin-bottom:0.75rem;box-shadow:0 1px 4px rgba(0,0,0,0.05);">
                    <div class="d-flex justify-content-between align-items-start mb-1">
                        <div class="fw-bold text-dark" style="font-size:0.88rem;line-height:1.3;">${item.name}</div>
                        <span style="background:${urgencyColor};color:white;font-size:0.65rem;font-weight:700;padding:2px 7px;border-radius:20px;white-space:nowrap;margin-left:8px;">${urgencyLabel}</span>
                    </div>
                    <div class="d-flex justify-content-between text-muted" style="font-size:0.78rem;">
                        <span>Stock: <strong style="color:${urgencyColor};">${item.currentStock}</strong> units</span>
                        <span>Min: <strong>${item.threshold}</strong> units</span>
                    </div>
                    <div style="background:#f1f5f9;border-radius:4px;height:5px;margin-top:6px;overflow:hidden;">
                        <div style="background:${urgencyColor};width:${barWidth}%;height:100%;border-radius:4px;"></div>
                    </div>
                </div>`;
            });
            list.innerHTML = html;
        },
        error: function () {
            _lowStockPanelLoaded = false; // allow retry
            if (list) list.innerHTML = '<div class="text-center text-muted py-5"><i class="fas fa-plug fa-2x mb-2 d-block text-muted"></i>Could not connect to server.<br><button class="btn btn-sm btn-outline-secondary mt-2" onclick="loadLowStockData()">Retry</button></div>';
        }
    });
}

// Silently load badge count 2 seconds after page load
$(document).ready(function () {
    setTimeout(function () {
        $.ajax({
            url: '/Inventory/GetLowStockAlerts',
            type: 'GET',
            dataType: 'json',
            success: function (res) {
                if (res && res.success) _updateLowStockBadge(res.count);
            }
        });
    }, 2000);
});

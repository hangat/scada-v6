﻿// Displays a scheme.
// Depends on jquery, bootstrap, scada-common.js, main-api.js, view-hub.js

// Namespaces
var scada = scada || {};
scada.scheme = scada.scheme || {};

// How long to show the error badge
const ERROR_DISPLAY_DURATION = 10000; // ms
// Used for testing
const DEBUG_MODE = false;
// Identifies the error badge display timeout
var errorTimeoutID = 0;
// Scheme object
var scheme = null;
// Possible scale values
var scaleVals = [0.1, 0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 2.5, 3, 4, 5];
// Provides access to current data
var mainApi = new MainApi();

// The variables below are set from SchemeView.cshtml
// View ID
var viewID = 0;
// Scheme refresh rate
var refrRate = 1000;
// Localized phrases
var phrases = {};
// View control right
var controlRight = false;
// Scheme options
var schemeOptions = scada.scheme.defaultOptions;

// Scheme environment object accessible by the scheme and its components
scada.scheme.env = {
    // Localized phrases
    phrases: phrases,
    // The view hub object
    viewHub: ViewHub.getInstance(),

    // Send telecommand
    sendCommand: function (ctrlCnlNum, cmdVal, viewID, componentID) {
        scheme.sendCommand(ctrlCnlNum, cmdVal, viewID, componentID, function (success) {
            if (!success) {
                console.error("Unable to send command");
                showErrorBadge();
                //notifier.addNotification(phrases.UnableSendCommand, scada.NotifTypes.ERROR, notifier.DEF_NOTIF_LIFETIME);
            }
        });
    }
};

// Load the scheme
function loadScheme(viewID) {
    scheme.load(viewID, function (success) {
        if (success) {
            if (!DEBUG_MODE) {
                // show errors
                if (Array.isArray(scheme.loadErrors) && scheme.loadErrors.length > 0) {
                    for (let err of scheme.loadErrors) {
                        console.error(err);
                    }

                    showErrorBadge();
                    /*notifier.addNotification(scheme.loadErrors.join("<br/>"),
                        scada.NotifTypes.ERROR, notifier.INFINITE_NOTIF_LIFETIME);*/
                }

                // show scheme
                scheme.createDom(controlRight);
                loadScale();
                displayScale();
                startUpdatingScheme();
            }
        } else {
            console.error("Error loading scheme");
            showErrorBadge();
            /*notifier.addNotification(phrases.LoadSchemeError +
                " <input type='button' value='" + phrases.ReloadButton + "' onclick='reloadScheme()' />",
                scada.NotifTypes.ERROR, notifier.INFINITE_NOTIF_LIFETIME);*/
        }
    });
}

// Reload the scheme
function reloadScheme() {
    location.reload(true);
}

// Start cyclic scheme updating process
function startUpdatingScheme() {
    scheme.updateData(mainApi, function (success) {
        if (!success) {
            console.error("Error updating scheme");
            showErrorBadge();
            //notifier.addNotification(phrases.UpdateError, scada.NotifTypes.ERROR, notifier.DEF_NOTIF_LIFETIME);
        }

        setTimeout(startUpdatingScheme, refrRate);
    });
}

// Bind handlers of the toolbar buttons
function initToolbar() {
    var ScaleTypes = scada.scheme.ScaleTypes;

    $("#lblFitScreenBtn").click(function () {
        scheme.setScale(ScaleTypes.FIT_SCREEN, 0);
        displayScale();
        saveScale();
    });

    $("#lblFitWidthBtn").click(function () {
        scheme.setScale(ScaleTypes.FIT_WIDTH, 0);
        displayScale();
        saveScale();
    });

    $("#lblZoomInBtn").click(function (event) {
        scheme.setScale(ScaleTypes.NUMERIC, getNextScale());
        displayScale();
        saveScale();
    });

    $("#lblZoomOutBtn").click(function (event) {
        scheme.setScale(ScaleTypes.NUMERIC, getPrevScale());
        displayScale();
        saveScale();
    });
}

// Get the previous scale value from the possible values array
function getPrevScale() {
    var curScale = scheme.scaleValue;
    for (var i = scaleVals.length - 1; i >= 0; i--) {
        var prevScale = scaleVals[i];
        if (curScale > prevScale) {
            return prevScale;
        }
    }
    return curScale;
}

// Get the next scale value from the possible values array
function getNextScale() {
    var curScale = scheme.scaleValue;
    for (var i = 0, len = scaleVals.length; i < len; i++) {
        var nextScale = scaleVals[i];
        if (curScale < nextScale) {
            return nextScale;
        }
    }
    return curScale;
}

// Display the scheme scale
function displayScale() {
    $("#spanCurScale").text(Math.round(scheme.scaleValue * 100) + "%");
}

// Load the scheme scale from the local storage
function loadScale() {
    var scaleType = NaN;
    var scaleValue = NaN;

    if (schemeOptions.rememberScale) {
        scaleType = parseInt(localStorage.getItem("Scheme.ScaleType"));
        scaleValue = parseFloat(localStorage.getItem("Scheme.ScaleValue"));
    }

    if (isNaN(scaleType) || isNaN(scaleValue)) {
        scheme.setScale(schemeOptions.scaleType, schemeOptions.scaleValue);
    } else {
        scheme.setScale(scaleType, scaleValue);
    }
}

// Save the scheme scale in the local storage
function saveScale() {
    localStorage.setItem("Scheme.ScaleType", scheme.scaleType);
    localStorage.setItem("Scheme.ScaleValue", scheme.scaleValue);
}

// Update the scheme scale if the scheme should fit size
function updateScale() {
    if (scheme.scaleType !== scada.scheme.ScaleTypes.NUMERIC) {
        scheme.setScale(scheme.scaleType, scheme.scaleValue);
        displayScale();
    }
}

// Update layout of the top level div elements
function updateLayout() {
    var divNotif = $("#divNotif");
    var divSchWrapper = $("#divSchWrapper");
    var divToolbar = $("#divToolbar");
    var notifHeight = divNotif.css("display") === "block" ? divNotif.outerHeight() : 0;
    var windowWidth = $(window).width();

    $("body").css("padding-top", notifHeight);
    divNotif.outerWidth(windowWidth);
    divSchWrapper
        .outerWidth(windowWidth)
        .outerHeight($(window).height() - notifHeight);
    divToolbar.css("top", notifHeight);
}

// Show error badge for a period of time
function showErrorBadge() {
    clearTimeout(errorTimeoutID);
    $("#spanErrorBadge").removeClass("hidden");

    errorTimeoutID = setTimeout(function () {
        $("#spanErrorBadge").addClass("hidden");
        errorTimeoutID = 0;
    }, ERROR_DISPLAY_DURATION);
}

// Initialize debug tools
function initDebugTools() {
    $("#divDebugTools").css("display", "inline-block");

    $("#spanLoadSchemeBtn").click(function () {
        loadScheme(viewID);
    });

    $("#spanCreateDomBtn").click(function () {
        scheme.createDom();
    });

    $("#spanStartUpdBtn").click(function () {
        startUpdatingScheme();
        $(this).prop("disabled", true);
    });

    $("#spanAddNotifBtn").click(function () {
        console.info("Test notification");
        /*notifier.addNotification(scada.utils.getCurTime() + " Test notification",
            scada.NotifTypes.INFO, notifier.DEF_NOTIF_LIFETIME);*/
    });
}

$(document).ready(function () {
    // setup client API
    //scada.clientAPI.rootPath = "../../";
    //scada.clientAPI.ajaxQueue = scada.ajaxQueueLocator.getAjaxQueue();

    // create scheme
    var divSchWrapper = $("#divSchWrapper");
    scheme = new scada.scheme.Scheme();
    scheme.schemeEnv = scada.scheme.env;
    scheme.parentDomElem = divSchWrapper;

    // setup user interface
    initToolbar();
    //scada.utils.styleIOS(divSchWrapper);
    updateLayout();
    //notifier = new scada.Notifier("#divNotif");
    //notifier.startClearingNotifications();

    $(window).on("resize " + ScadaEventType.UPDATE_LAYOUT, function () {
        updateLayout();
        updateScale();
    });

    if (DEBUG_MODE) {
        initDebugTools();
    } else {
        loadScheme(viewID);
    }
});

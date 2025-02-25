﻿// The variables below are set from Command.cshtml
var hideExecBtn = false;
var updateHeight = false;
var closeModal = false;

const CLOSE_TIMEOUT = 1000;

$(document).ready(function () {
    // hide Execute button
    let modalManager = ModalManager.getInstance();

    if (hideExecBtn) {
        modalManager.setButtonVisible(window, ModalButton.EXEC, false);
    }

    // update modal height
    if (updateHeight) {
        modalManager.updateModalHeight(window, true);
    }

    // close modal
    if (closeModal) {
        setTimeout(function () {
            modalManager.closeModal(window, true);
        }, CLOSE_TIMEOUT)
    }

    // submit the form on Execute button click
    $(window).on(ScadaEventType.MODAL_BTN_CLICK, function (event, buttonValue) {
        if (buttonValue === ModalButton.EXEC) {
            $("#btnSubmit").click();
        }
    });

    // send enumeration command
    $("#divEnum button").click(function () {
        $("#hidCmdEnum").val($(this).data("cmdval"));
        $("#btnSubmit").click();
    });

    // reset invalid state
    $("input.is-invalid, textarea.is-invalid").on("input", function () {
        $(this).removeClass("is-invalid");
    });
});

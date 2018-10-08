var ws;
let pattern = /^\w{1,16}$/;
let patternErrorText = "This field must match the regex '" + pattern + "'.";
let existingErrorText = "This login is already used.";
let incorrectPairErrorText = "Incorrect login or password.";

function login() {
    let loginField = $('#l-fld');
    let passwordField = $('#p-fld');
    if (!loginField.hasClass("is-invalid") && !passwordField.hasClass("is-invalid")) {
        ws.onmessage = function (e) {
            if (e.data === "true") {
                window.location.replace("/login?login=" + loginField.val() + "&password=" + passwordField.val());
            } else {
                loginField.addClass("is-invalid");
                let loginFieldId = loginField.attr("id");
                let errorField = $("#" + loginFieldId.substring(0, loginFieldId.length - 4) + "-err");
                errorField.text(incorrectPairErrorText);
            }
        };
        ws.send('validate{"Login": "' + loginField.val() + '", "Password": "' + passwordField.val() + '"}');
    }
    return false;
}

function resetImproprietyError() {
    let loginField = $('#l-fld');
    let errorField = $('#l-err');
    if (errorField.text() === incorrectPairErrorText) {
        loginField.removeClass("is-invalid");
        errorField.text("");
    }
}

function register() {
    let loginField = $('#r-l-fld');
    let passwordField = $('#p-fld');
    if (!loginField.hasClass("is-invalid") && !passwordField.hasClass("is-invalid")) {
        window.location.replace("/register?login=" + loginField.val() + "&password=" + passwordField.val());
    }
    return false;
}

function checkLoginExisting() {
    let rLoginField = $('#r-l-fld');
    let rLoginError = $('#r-l-err');
    ws.onmessage = function (e) {
        if (e.data === "true") {
            rLoginField.addClass("is-invalid");
            rLoginError.text(existingErrorText);
        } else if (rLoginError.text() === existingErrorText) {
            rLoginField.removeClass("is-invalid");
            rLoginError.text("");
        }
    };
    ws.send('check-existence{"Login": "' + rLoginField.val() + '"}');
}

function checkPattern() {
    let thisId = $(this).attr("id");
    let errorField = $("#" + thisId.substring(0, thisId.length - 4) + "-err");
    if ($(this).val().match(pattern) == null) {
        errorField.text(patternErrorText);
        $(this).addClass("is-invalid");
    } else if (errorField.text() === patternErrorText) {
        errorField.text("");
        $(this).removeClass("is-invalid");
    }
}

function enableButton() {
    $('button[type="submit"]').prop("disabled", false);
}

$(document).ready(function () {
    let loginFld = $("#l-fld");
    let rLoginFld = $("#r-l-fld");
    let passwordFld = $("#p-fld");
    loginFld.on("change", checkLoginExisting);
    loginFld.on("change", checkPattern);
    loginFld.on("change", resetImproprietyError);
    loginFld.on("change", enableButton);
    rLoginFld.on("change", checkLoginExisting);
    rLoginFld.on("change", checkPattern);
    rLoginFld.on("change", enableButton);
    passwordFld.on("change", checkPattern);
    passwordFld.on("change", enableButton);
    ws = new WebSocket("ws://" + location.host + "/isreg");
});

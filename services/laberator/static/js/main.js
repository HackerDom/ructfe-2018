var ws;
let pattern = /^\w{1,16}$/;
let patternErrorText = "This field must match the regex " + pattern;
let existingErrorText = "This login is already used.";

function login() {
    let loginField = $('#l-fld');
    let passwordField = $('#p-fld');
    if (!loginField.hasClass("is-invalid") && !passwordField.hasClass("is-invalid")) {
        window.location.replace("/login?login=" + loginField.val() + "&password=" + passwordField.val());
    }
    return false;
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
        console.log(e.data === "true");
        if (e.data === "true") {
            rLoginField.addClass("is-invalid");
            rLoginError.text(existingErrorText);
        } else if (rLoginError.text() === existingErrorText) {
            rLoginField.removeClass("is-invalid");
            rLoginError.text("");
        }
    };
    ws.send(rLoginField.val());
}

function validatePair() {
    ws.onmessage = function (e) {
        console.log(e.data === "true");
        if (e.data === "true") {
            rLoginField.addClass("is-invalid");
            rLoginError.text(existingErrorText);
        } else if (rLoginError.text() === existingErrorText) {
            rLoginField.removeClass("is-invalid");
            rLoginError.text("");
        }
    };
    ws.send(rLoginField.val());
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

$(document).ready(function () {
    let loginBtn = $('#login-btn');
    let loginFld = $("#l-fld");
    let rLoginFld = $("#r-l-fld");
    loginBtn.on("click", login);
    $('#register-btn').on("click", register);
    loginFld.on("change", checkLoginExisting);
    loginFld.on("change", checkPattern);
    rLoginFld.on("change", checkLoginExisting);
    rLoginFld.on("change", checkPattern);
    $("#p-fld").on("change", checkPattern);
    ws = new WebSocket("ws://" + location.host + "/isreg");
});

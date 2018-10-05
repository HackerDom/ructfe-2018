var ws;

function login() {
    let loginField = $('#login-fld');
    let passwordField = $('#password-fld');
    window.location.replace("/login?login=" + loginField.val() + "&password=" + passwordField.val());
}

function register() {
    let loginField = $('#login-fld');
    let passwordField = $('#password-fld');
    window.location.replace("/register?login=" + loginField.val() + "&password=" + passwordField.val());
}

function checkLoginExisting() {
    let rLoginField = $('#r-login-fld');
    ws.onmessage = function (e) {
        if (e.data === "true") {
            rLoginField.tooltip({
                trigger: "manual",
                title: "This login is already exist."
            }).tooltip("show");
        } else {
            rLoginField.tooltip("hide");
        }
    };
    ws.send(rLoginField.val());
}

$(document).ready(function () {
    let loginBtn = $('#login-btn');
    loginBtn.on("click", login);
    let registerBtn = $('#register-btn');
    registerBtn.on("click", register);
    $("#r-login-fld").on("change", checkLoginExisting);
    ws = new WebSocket("ws://" + location.host + "/isreg");
});

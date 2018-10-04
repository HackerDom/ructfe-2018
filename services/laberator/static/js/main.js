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

function checkLogin() {
    let loginField = $('#login-fld');
    ws.onmessage = function (e) {
        alert(e.data);
    };
    ws.send(loginField.val());
}

$(document).ready(function () {
    let loginBtn = $('#login-btn');
    loginBtn.on("click", login);
    let registerBtn = $('#register-btn');
    registerBtn.on("click", register);
    let registerFld = $("#login-fld");
    registerFld.on("change", checkLogin);
    ws = new WebSocket("ws://0.0.0.0:8080/isreg");
});

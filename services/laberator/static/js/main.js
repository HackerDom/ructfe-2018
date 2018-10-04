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

$(document).ready(function () {
    let loginBtn = $('#login-btn');
    loginBtn.on("click", login);
    let registerBtn = $('#register-btn');
    registerBtn.on("click", register);
});

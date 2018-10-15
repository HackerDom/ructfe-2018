var ws;
let pattern = /^\w{1,16}$/;
let patternErrorText = "This field must match the regex '" + pattern + "'.";
let existingErrorText = "This login is already used.";
let incorrectPairErrorText = "Incorrect login or password.";
let fontPattern = /^[\w\s]{1,100}$/;
let invalidFontTextError = "Label font must match the regex '" + fontPattern + "'.";
let tooLargeLabelTextError =  "Label text can not be greater than 100 symbols";
let invalidLabelSizeError =  "Label size must be in range [10, 100]";

function waitSocket(socket, callback) {
    setTimeout(
        function () {
            if (socket.readyState === 1) {
                callback();
            } else {
                waitSocket(socket, callback);
            }
        },
    5);
}

function getErrorField(fieldId) {
    return $("#" + fieldId.substring(0, fieldId.length - 4) + "-err");
}

function setError(field, errorText) {
    field.addClass("is-invalid");
    let fieldId = field.attr("id");
    let errorField = getErrorField(fieldId);
    errorField.text(errorText);
}

function unsetError(field, errorText) {
    let fieldId = field.attr("id");
    let errorField = getErrorField(fieldId);
    if (errorField.text() === errorText) {
        field.removeClass("is-invalid");
        errorField.text("");
    }
}

function createCommandRequest(command, data) {
    return JSON.stringify({
        "Command": command,
        "Data": JSON.stringify(data),
    });
}

function login() {
    let loginField = $('#l-fld');
    let passwordField = $('#p-fld');
    if (!loginField.hasClass("is-invalid") && !passwordField.hasClass("is-invalid")) {
        ws.onmessage = function (e) {
            if (e.data === "true") {
                window.location.replace("/login?login=" + loginField.val() + "&password=" + passwordField.val());
            } else {
                setError(loginField, incorrectPairErrorText);
            }
        };
        ws.send(createCommandRequest("validate", {
            "Login": loginField.val(),
            "Password": passwordField.val()
        }));
    }
    return false;
}

function resetImproprietyError() {
    let loginField = $('#l-fld');
    unsetError(loginField, incorrectPairErrorText);
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
    ws.onmessage = function (e) {
        if (e.data === "true") {
            setError(rLoginField, existingErrorText);
        } else {
            unsetError(rLoginField, existingErrorText);
        }
    };
    ws.send(createCommandRequest("check-existence", {
        "Login": rLoginField.val()
    }));
}

function checkPattern() {
    if ($(this).val().match(pattern) == null) {
        setError($(this), patternErrorText);
    } else {
        unsetError($(this), patternErrorText);
    }
}

function enableButton() {
    $('button[type="submit"]').prop("disabled", false);
}

function validateLabel() {
    let textFld = $("#t-fld");
    let fontFld = $("#f-fld");
    let sizeFld = $("#s-fld");
    if (textFld.val().length > 100) {
        setError(textFld, tooLargeLabelTextError);
        return false;
    } else {
        unsetError(textFld, tooLargeLabelTextError);
    }
    if (!fontFld.val().match(fontPattern)) {
        setError(fontFld, invalidFontTextError);
        return false;
    } else {
        unsetError(fontFld, invalidFontTextError);
    }
    if (Number(sizeFld.val()) < 0 || Number(sizeFld.val()) > 100) {
        setError(sizeFld, invalidLabelSizeError);
        return false;
    } else {
        unsetError(sizeFld, invalidLabelSizeError);
    }
    return true;
}

function createLabel() {
    if (!validateLabel()) {
        return false;
    }
    let textFld = $("#t-fld");
    let fontFld = $("#f-fld");
    let sizeFld = $("#s-fld");
    ws.onmessage = function (e) {
        if (e.data === "true") {
            alert("Label has been successfully created");
        } else {
            alert("Label has not been successfully created");
        }
        window.location.replace("/");
    };
    ws.send(createCommandRequest("create", {
        "RawCookies": document.cookie,
        "Text": textFld.val(),
        "Font": fontFld.val(),
        "Size": Number(sizeFld.val())
    }));
}

function extendTable() {
    waitSocket(ws, function(){
        ws.onmessage = function (e) {
            let tableElements = JSON.parse(e.data);
            tableElements.forEach(function (label) {
                let textTd = document.createElement('td');
                let fontTd = document.createElement('td');
                let sizeTd = document.createElement('td');
                let tr = document.createElement("tr");
                let link = document.createElement("a");
                link.setAttribute("href", "/labels/" + label.ID);
                link.innerText = label.Text;
                textTd.innerHTML = $(link).prop("outerHTML");
                fontTd.innerText = label.Font;
                sizeTd.innerText = label.Size;

                tr.appendChild(textTd);
                tr.appendChild(fontTd);
                tr.appendChild(sizeTd);
                $("#l-t").append(tr);
            });
            if (tableElements.length === 0) {
                $("#e-b").remove();
            }
        };
        ws.send(createCommandRequest("list", {
            "RawCookies": document.cookie,
            "Offset": $("#l-t tr").length
        }));
    });
}

$(document).ready(function () {
    let loginFld = $("#l-fld");
    let rLoginFld = $("#r-l-fld");
    let passwordFld = $("#p-fld");
    let labelTextFld = $("#t-fld");
    let fontFld = $("#f-fld");
    let sizeFld = $("#s-fld");
    loginFld.on("change", checkPattern);
    loginFld.on("change", resetImproprietyError);
    loginFld.on("change", enableButton);
    rLoginFld.on("change", checkLoginExisting);
    rLoginFld.on("change", checkPattern);
    rLoginFld.on("change", enableButton);
    passwordFld.on("change", checkPattern);
    passwordFld.on("change", enableButton);
    labelTextFld.on("change", validateLabel);
    fontFld.on("change", validateLabel);
    sizeFld.on("change", validateLabel);
    ws = new WebSocket("ws://" + location.host + "/cmdexec");
});

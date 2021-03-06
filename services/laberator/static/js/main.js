let ws;
let pattern = /^\w{1,40}$/;
let phrasePattern = /^[a-zA-Z0-9!@#$%&*()_+=/., ]{1,100}$/;
let patternErrorText = "This field must match the regex '" + pattern + "'.";
let phrasePatternErrorText = "This field must match the regex '" + phrasePattern + "'.";
let existingErrorText = "This login is already used.";
let incorrectPairErrorText = "Incorrect login or password.";
let fontPattern = /^[\w\s]{1,100}$/;
let invalidFontTextError = "Label font must match the regex '" + fontPattern + "'.";
let tooLargeLabelTextError =  "Label text can not be greater than 40 symbols";
let invalidLabelSizeError =  "Label size must be in range [10, 80]";

function waitSocket(socket, callback) {
    setTimeout(
        function () {
            let done = false;
            if (socket) {
                if (socket.readyState === 1) {
                    callback();
                    done = true;
                }
            }
            if (!done) {
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
                window.location.replace(
                    "/login?login=" + loginField.val() +
                    "&password=" + btoa(passwordField.val())
                );
            } else {
                setError(loginField, incorrectPairErrorText);
            }
        };
        ws.send(createCommandRequest("validate", {
            "Login": loginField.val(),
            "Password": btoa(passwordField.val())
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
    let phraseField = $('#r-s-fld');
    if (!loginField.hasClass("is-invalid") && !passwordField.hasClass("is-invalid") && !phraseField.hasClass("is-invalid")) {
        window.location.replace(
            "/register?login=" + loginField.val() +
            "&password=" + btoa(passwordField.val()) +
            "&phrase=" + btoa(phraseField.val())
        );
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

function checkSecretPattern() {
    if ($(this).val().match(phrasePattern) == null) {
        setError($(this), phrasePatternErrorText);
    } else {
        unsetError($(this), phrasePatternErrorText);
    }
}

function enableButton() {
    $('button[type="submit"]').prop("disabled", false);
}

function validateLabelWithErrors() {
    let textFld = $("#t-fld");
    let fontFld = $("#f-fld");
    let sizeFld = $("#s-fld");
    if (textFld.val().length > 40) {
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
    if (Number(sizeFld.val()) < 0 || Number(sizeFld.val()) > 80) {
        setError(sizeFld, invalidLabelSizeError);
        return false;
    } else {
        unsetError(sizeFld, invalidLabelSizeError);
    }
    return true;
}

function getLoginFromCookies() {
    let result = "";
    document.cookie.split("; ").forEach(function (rawCookie) {
        let cookie = rawCookie.split("=");
        if (cookie[0] === "login") {
            result = cookie[1];
        }
    });
    return result;
}

function validateLabel(text, font, size) {
    if (text.length > 40)
        return false;
    if (!font.match(fontPattern))
        return false;
    return !(Number(size) < 0 || Number(size) > 80);
}

function createLabel() {
    if (!validateLabelWithErrors()) {
        return false;
    }
    let textFld = $("#t-fld");
    let fontFld = $("#f-fld");
    let sizeFld = $("#s-fld");
    ws.onmessage = function (e) {
        window.location.replace("/");
    };
    ws.send(createCommandRequest("create", {
        "RawCookies": document.cookie,
        "Text": textFld.val(),
        "Font": fontFld.val(),
        "Size": Number(sizeFld.val())
    }));
}

function viewLabel(labelId) {
    waitSocket(ws, function() {
        ws.onmessage = function (e) {
            let label = JSON.parse(e.data);
            if (label.Owner !== getLoginFromCookies()) {
                return;
            }
            let canvas = $("#l-c")[0];

            let context = canvas.getContext("2d");
            let image = $("#l-i")[0];
            context.font = label.Size + "px " + label.Font;
            context.fillText(label.Text, 0, 100);
            image.src = canvas.toDataURL();
            if (!validateLabel(label.Text, label.Font, label.Size)) {
                return;
            }
            $("#l-text").text("Text: " + label.Text);
            $("#l-size").text("Size: " + label.Size);
            $("#l-font").text("Font: " + label.Font);
        };
        ws.send(createCommandRequest("view", {
            "LabelId": labelId,
            "RawCookies": document.cookie,
        }));
    });
}

function fillTableElements(tableEls, objectToAppend) {
    tableEls.forEach(function (user) {
        let td = document.createElement('td');
        td.innerText = user.Login;
        let tr = document.createElement("tr");
        tr.appendChild(td);
        objectToAppend.append(tr);
    });
}

function fillLastUsers() {
    waitSocket(ws, function() {
        ws.onmessage = function (e) {
            let tableElements = JSON.parse(e.data);
            let firstTable = $("#u-t-1");
            let secondTable = $("#u-t-2");
            firstTable.empty();
            secondTable.empty();
            let firstTableElements = tableElements.slice(0, 10);
            let secondTableElements = tableElements.slice(10, 20);
            fillTableElements(firstTableElements, firstTable);
            fillTableElements(secondTableElements, secondTable);
        };
        ws.send(createCommandRequest("last_users", {}));
    });
}

function extendTable() {
    waitSocket(ws, function() {
        ws.onmessage = function (e) {
            let tableElements = JSON.parse(e.data);
            tableElements.forEach(function (label) {
                let textTd = document.createElement('td');
                let fontTd = document.createElement('td');
                let sizeTd = document.createElement('td');
                if (!validateLabel(label.Text, label.Font, label.Size)) {
                    return;
                }
                if (!typeof(label.ID)) {
                    return;
                }
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

function setPollingInterval() {
    setInterval(fillLastUsers, 1000);
    fillLastUsers();
}

$(document).ready(function () {
    let loginFld = $("#l-fld");
    let rLoginFld = $("#r-l-fld");
    let passwordFld = $("#p-fld");
    let labelTextFld = $("#t-fld");
    let fontFld = $("#f-fld");
    let sizeFld = $("#s-fld");
    let phraseFld = $("#r-s-fld");
    loginFld.on("change", checkPattern);
    loginFld.on("change", resetImproprietyError);
    loginFld.on("change", enableButton);
    phraseFld.on("change", checkSecretPattern);
    rLoginFld.on("change", checkLoginExisting);
    rLoginFld.on("change", checkPattern);
    rLoginFld.on("change", enableButton);
    passwordFld.on("change", checkPattern);
    labelTextFld.on("change", validateLabelWithErrors);
    fontFld.on("change", validateLabelWithErrors);
    sizeFld.on("change", validateLabelWithErrors);
    ws = new WebSocket("ws://" + location.host + "/cmdexec");
});

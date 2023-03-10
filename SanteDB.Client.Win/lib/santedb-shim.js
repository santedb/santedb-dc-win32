/*
 * Copyright 2023 SanteSuite Inc. Windows
 */
if (typeof __SanteDBAppService === 'undefined') {
    console.log("reinstantiating __SanteDBAppService");
    window.__SanteDBAppService = {};
}

const __sdbthis = this;
let __firstrun = true;

window.fetch("/_appservice/strings").then((response) => {
    response.json().then((dictionary) => {
        __SanteDBAppService.strings = dictionary;
    }, (reason) => {
        alert("failed to decode string dictionary: " + reason);
    });

}, (reason) => {
    alert("Failed to fetch string dictionary: " + reason);
})

__SanteDBAppService.GetStatus = async function () {
    const response = await window.fetch("/_appservice/state");
    return await response.json();
};

(function updateState() {
    setTimeout(async () => {
        __SanteDBAppService._state = await __SanteDBAppService.GetStatus();

        updateState();
    }, __firstrun ? 1 : 1000);
    __firstrun = false;
})()

__SanteDBAppService.GetVersion = function () {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.version;
    }
    else{
        return null;
    }
};

__SanteDBAppService.Print = function () {
    window.print();
};

__SanteDBAppService.Close = function () {
    window.console.log("Close function called.");
};

__SanteDBAppService.GetDataAsset = function (dataId) {

    return null;
};

if (window && window.crypto && window.crypto.randomUUID) {
    __SanteDBAppService.NewGuid = (function () {
        return this.crypto.randomUUID();
    }).bind(__sdbthis);
}
else {
    __SanteDBAppService.NewGuid = function () {
        let chars = "0123456789abcdef";
        let arr = [];

        for (let i = 0; i < 32; i++) {
            arr.push(chars.at(Math.random() * chars.length));
        }
        arr.splice(20, 0, '-')
        arr.splice(16, 0, '-')
        arr.splice(12, 0, '-')
        arr.splice(8, 0, '-')

        return arr.join('');
    };
}

__SanteDBAppService.GetOnlineState = function() {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.online;
    }
    else{
        return false;
    }
};

__SanteDBAppService.IsAdminAvailable = function() {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.ami;
    }
    else{
        return false;
    }
};

__SanteDBAppService.IsClinicalAvailable = function() {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.hdsi;
    }
    else{
        return false;
    }
};

__SanteDBAppService.BarcodeScan = function(){
    return null;
};

__SanteDBAppService.ShowToast = function (text) {

    const toastdata = {
        "version": 1,
        "title": null,
        "text": text,
        "icon": null
    };

    return window.fetch("_appservice/toast", {
        method: "POST",
        mode: "cors",
        cache: "no-cache",
        credentials: "same-origin",
        headers: {
            "Content-Type": "application/json"
        },
        redirect: "follow",
        referrerPolicy: "no-referrer",
        body: JSON.stringify(toastdata)
    });
};

__SanteDBAppService.GetClientId = function() {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.client_id;
    }
    else{
        return null;
    }
};

__SanteDBAppService.GetDeviceId = function() {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.device_id;
    }
    else{
        return null;
    }
}

__SanteDBAppService.GetRealm = function() {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.realm;
    }
    else{
        return null;
    }
};

__SanteDBAppService.GetLocale = function() {
    if (window.sessionStorage.lang)
        return window.sessionStorage.lang;
    else
        return (navigator.language || navigator.userLanguage).substring(0, 2);
};

__SanteDBAppService.SetLocale = function(locale) {
    window.sessionStorage.lang = locale;
};

__SanteDBAppService.GetString = function(stringId) {
    if (__SanteDBAppService && __SanteDBAppService.strings) {
        return __SanteDBAppService.strings[stringId];
    }
    else {
        return stringId;
    }
};

__SanteDBAppService.GetMagic = function() {
    if (__SanteDBAppService && __SanteDBAppService._state){
        return __SanteDBAppService._state.magic;
    }
    else{
        return null;
    }
};


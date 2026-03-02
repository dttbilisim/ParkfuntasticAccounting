window.EczaPro = {
    downloadFileFromStream: async (fileName, contentStreamReference) => {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer]);
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? '';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    },

    loadedScripts: [],

    loadScript: (scriptPath) => {
        if (EczaPro.loadedScripts[scriptPath]) {
            return new Promise(function (resolve) {
                resolve();
            });
        }

        return new Promise(function (resolve, reject) {
            let script = document.createElement("script");
            script.src = scriptPath;
            script.type = "text/javascript";

            EczaPro.loadedScripts[scriptPath] = true;

            script.onload = function () {
                resolve(scriptPath);
            };

            script.onerror = function () {
                console.log(scriptPath + " load failed");
                reject(scriptPath);
            }

            document["body"].appendChild(script);
        });
    },

    getRecaptchaResponse: async (siteKey) => {
        if (typeof grecaptcha === 'undefined') {
            return;
        }

        await grecaptcha.ready(function () {
        });

        return await grecaptcha.execute(siteKey, { action: 'submit' });
    },

    openedPopups: [],

    popupClickHandler: function (popup, e) {
        e.preventDefault();

        $(this).off();

        EczaPro.openPopup(popup, function () {
            e.currentTarget.click();
        });
    },

    initPopups: function (popups) {
        if (typeof $.fancybox === 'undefined' || !Array.isArray(popups) || popups.length === 0) {
            return;
        }

        EczaPro.openedPopups = [];

        popups.forEach(function (popup) {
            if (popup.trigger === 0) {
                setTimeout(function () {
                    EczaPro.openPopup(popup);
                }, 300)
            } else if (popup.trigger === 1 && popup.triggerReference) {
                let cliclEl = $(popup.triggerReference);

                if (cliclEl.length > 0) {
                    cliclEl.off('click', EczaPro.popupClickHandler);
                    cliclEl.one('click', EczaPro.popupClickHandler.bind(cliclEl, popup));
                }
            }
        });
    },

    openPopup: function (data, afterClose = null) {
        if (EczaPro.openedPopups[data.id]) {
            return;
        }

        EczaPro.openedPopups[data.id] = true;

        let html = $(`<div class='popup-content'>`);

        if (data.title) {
            html.append(`<h3 class='popup-title'>${data.title}</h3>`);
        }

        html.append(data.body);

        html.find('img').css('max-width', '100%').css('height', 'auto');

        if (data.isOnlyImage) {
            html.css('max-width', '100%').css('height', 'auto').css('padding', '0');
        }

        return $.fancybox.open(html, {
            keyboard: false,
            smallBtn: true,
            protect: true,
            touch: false,
            buttons: [],
            afterClose: afterClose,
            afterLoad: function (instance, current) {
                let content = $(current.$content);

                content.closest('.fancybox-container').find('.fancybox-bg').css('opacity', '0.2');

                if (data.width) {
                    content.css('max-width', '100%');
                    content.css('width', data.width);
                }

                if (data.height) {
                    content.css('height', data.height);
                }
            },
        });
    },

    scrollToBottom: function () {
        window.scrollTo({
            top: document.body.scrollHeight,
            behavior: 'smooth'
        });
    },

    scrollCheckoutToBottom: function () {
        setTimeout(function () {
            // Target RadzenBody which usually has the scrollbar
            const body = document.querySelector('.rz-body') || document.documentElement || document.body;
            body.scrollTo({
                top: body.scrollHeight,
                behavior: 'smooth'
            });

            // Just in case window is also scrolling
            window.scrollTo({
                top: document.documentElement.scrollHeight,
                behavior: 'smooth'
            });
        }, 200);
    },

    submit3DSecureForm: function (formContent, iframeName) {
        console.log("submit3DSecureForm: Starting for iframe", iframeName);

        // Remove existing container if any
        let container = document.getElementById('temp-payment-container');
        if (container) { container.remove(); }

        container = document.createElement('div');
        container.id = 'temp-payment-container';
        container.style.display = 'none';

        // CRITICAL: Inject target attribute into the HTML string BEFORE it touches the DOM
        // This stops auto-submit scripts that run immediately on injection from hijacking the window
        let cleanedContent = formContent;
        if (cleanedContent.toLowerCase().includes('target=')) {
            cleanedContent = cleanedContent.replace(/target=["'][^"']*["']/gi, `target="${iframeName}"`);
        } else {
            cleanedContent = cleanedContent.replace(/<form/gi, `<form target="${iframeName}"`);
        }

        container.innerHTML = cleanedContent;
        document.body.appendChild(container);

        const form = container.querySelector('form');
        if (form) {
            form.target = iframeName; // Secondary safety

            let attempts = 0;
            const trySubmit = () => {
                const iframe = document.querySelector(`iframe[name="${iframeName}"]`) || document.getElementById(iframeName);

                if (iframe) {
                    console.log(`submit3DSecureForm: Iframe found after ${attempts} attempts. Submitting...`);
                    form.submit();
                    // Keep container for a bit to ensure submission completes
                    setTimeout(() => { if (container.parentNode) container.remove(); }, 2000);
                } else if (attempts < 50) { // 5 seconds max
                    attempts++;
                    if (attempts % 10 === 0) console.log(`submit3DSecureForm: Still waiting for iframe ${iframeName}... (${attempts})`);
                    setTimeout(trySubmit, 100);
                } else {
                    console.error('submit3DSecureForm: Target iframe not found after 5 seconds, submitting anyway (will likely open new tab).');
                    form.submit();
                }
            };

            trySubmit();
        } else {
            console.error('submit3DSecureForm: No form found in 3D Secure content');
        }
    }
};

// Alias for ecommerce namespace (for backward compatibility)
window.ecommerce = window.EczaPro;

// Base64 PDF indirme fonksiyonu (e-Fatura için)
window.downloadFileFromBase64 = function (base64Data, fileName, mimeType) {
    var byteCharacters = atob(base64Data);
    var byteNumbers = new Array(byteCharacters.length);
    for (var i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    var byteArray = new Uint8Array(byteNumbers);
    var blob = new Blob([byteArray], { type: mimeType });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};

// HTML içeriğini yeni sekmede açma fonksiyonu (e-Fatura önizleme için)
window.openHtmlInNewTab = function (htmlContent) {
    var newWindow = window.open('', '_blank');
    if (newWindow) {
        newWindow.document.write(htmlContent);
        newWindow.document.close();
    }
};

window.DispatchChangeEvent = function (elementName) {
    const e = new Event("change");
    const element = document.getElementsByName(elementName)[0]

    if (element !== null) {
        element.dispatchEvent(e);
    } else {
        console.error(`Element with id '${elementId}' not found.`);
    }
}
const secretKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-={}[]|:;<>,.?/~`";
function setCookie(name, value, days) {

    const encryptedData = CryptoJS.AES.encrypt(JSON.stringify(value), secretKey).toString();

    let expires = "";
    if (days) {
        const date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }

    const secure = location.protocol === "https:" ? "; Secure" : "";


    document.cookie = `${encodeURIComponent(name)}=${encryptedData}${expires}; path=/; SameSite=Lax${secure}`;
}

function getCookie(name) {
    const nameEQ = encodeURIComponent(name) + "=";
    const cookies = document.cookie.split(';');

    for (let i = 0; i < cookies.length; i++) {
        let cookie = cookies[i].trim();
        if (cookie.indexOf(nameEQ) === 0) {
            const encryptedData = cookie.substring(nameEQ.length, cookie.length);
            try {
                // AES ile şifreyi çözüyoruz
                const bytes = CryptoJS.AES.decrypt(encryptedData, secretKey);
                const decryptedData = bytes.toString(CryptoJS.enc.Utf8);
                return JSON.parse(decryptedData);
            } catch (error) {
                console.error("Şifre çözme başarısız:", error);
                return null;
            }
        }
    }

    return null;
}
function deleteCookie(name, path = "/", domain = "") {
    const secure = location.protocol === "https:" ? "; Secure" : "";
    const domainAttribute = domain ? `; domain=${domain}` : "";

    document.cookie = `${encodeURIComponent(name)}=; Max-Age=0; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=${path}${domainAttribute}; SameSite=Lax${secure}`;
}
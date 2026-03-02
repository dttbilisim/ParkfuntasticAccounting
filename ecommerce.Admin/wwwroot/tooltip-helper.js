// Radzen Tooltip Helper
// Gets ElementReference from element ID for tooltip usage

window.getElementReference = function(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        return DotNet.createJSObjectReference(element);
    }
    return null;
};

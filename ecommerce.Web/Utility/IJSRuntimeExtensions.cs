using Microsoft.JSInterop;

namespace ecommerce.Web.Utility;

public static class IJSRuntimeExtensions{
    public static ValueTask SaveAs(this IJSRuntime js, string fileName, byte[] content){return js.InvokeVoidAsync("saveAsFile", fileName, Convert.ToBase64String(content));}
    public static ValueTask<object> LoaderShow(this IJSRuntime js){return js.InvokeAsync<object>("HoldOn.open");}
    public static ValueTask<object> LoaderClose(this IJSRuntime js){return js.InvokeAsync<object>("HoldOn.close");}
    public static ValueTask DisplayMessage(this IJSRuntime js, string message){return js.InvokeVoidAsync("Swal.fire", message);}
    public static ValueTask Alertfy(this IJSRuntime js, string message, string type){return js.InvokeVoidAsync("alertify.notify", message, type, 2);}
    public static ValueTask SetInLocalStorage(this IJSRuntime js, string key, string content) => js.InvokeVoidAsync("localStorage.setItem", key, content);
    public static ValueTask<string> GetFromLocalStorage(this IJSRuntime js, string key) => js.InvokeAsync<string>("localStorage.getItem", key);
    public static ValueTask RemoveItem(this IJSRuntime js, string key) => js.InvokeVoidAsync("localStorage.removeItem", key);
    public static ValueTask ShowFullPageLoader(this IJSRuntime js)
        => js.InvokeVoidAsync("fullPageLoader.show");

    public static ValueTask HideFullPageLoader(this IJSRuntime js)
        => js.InvokeVoidAsync("fullPageLoader.hide");
}
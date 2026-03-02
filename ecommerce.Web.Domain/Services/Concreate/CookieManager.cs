using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.JSInterop;

namespace ecommerce.Web.Domain.Services.Concreate;

public class CookieManager(IJSRuntime _jsRuntime) : ICookieManager{
    public async Task SetCookie(string key, string value, int day){
        try{
            await _jsRuntime.InvokeVoidAsync("setCookie", key, value, day);
            Console.WriteLine($"Cookie set: {key} = {value}");
        } catch(Exception e){
            Console.WriteLine($"Error setting cookie: {e.Message}");
        }
    }
    public async Task<string> GetCookie(string key){
        var value = await _jsRuntime.InvokeAsync<string>("getCookie", key);
        if(value == null){

        }
        return value;
    }
    public async Task DeleteCookie(string key){
        var value = await _jsRuntime.InvokeAsync<string>("getCookie", key);
        if(value == null){
            throw new InvalidOperationException("HTTP context is not available.");
        }
        await _jsRuntime.InvokeVoidAsync("deleteCookie", key);
    }
    public async Task<bool> IsUserLoggedIn(){
        var rs = await GetCookie("Email");
        return !string.IsNullOrEmpty(rs);
    }
    public async Task<string> GetFullNAme(){
        var rs = await GetCookie("FullName");
        return rs;
    }
    public async Task<int> GetUserId(){
        var rsvalue = 0;
        var rs=await GetCookie("Id");
        if(!string.IsNullOrEmpty(rs)){
            rsvalue = Convert.ToInt32(rs);
        }
        return rsvalue;
    }
}
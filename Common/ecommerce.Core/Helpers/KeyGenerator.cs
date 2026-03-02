using System.Security.Cryptography;
using System.Text;

namespace ecommerce.Core.Helpers;
public class KeyGenerator{
    internal static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
    public static string GetUniqueKey(int size){
        var data = new byte[4 * size];
        using(var crypto = RandomNumberGenerator.Create()){
            crypto.GetBytes(data);
        }
        var result = new StringBuilder(size);
        for(var i = 0; i < size; i ++){
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % chars.Length;
            result.Append(chars[idx]);
        }
        return result.ToString();
    }
    public static string GetUniqueKeyOriginal_BIASED(int size){
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
        var data = new byte[size];
        using(var crypto = new RNGCryptoServiceProvider()){
            crypto.GetBytes(data);
        }
        var result = new StringBuilder(size);
        foreach(var b in data){
            result.Append(chars[b % (chars.Length)]);
        }
        return result.ToString();
    }
}
namespace ecommerce.Admin.Services.Dtos
{
    /// <summary>
    /// Radzen DropDown Data için tip güvenli seçim öğesi. Data olarak IEnumerable&lt;object&gt; kullanıldığında
    /// Radzen 'Value' is not a member of type 'System.Object' hatası verir; bu DTO ile tip belirtilir.
    /// </summary>
    public class SelectItemDto<T>
    {
        public string Text { get; set; } = string.Empty;
        public T? Value { get; set; }
    }
}

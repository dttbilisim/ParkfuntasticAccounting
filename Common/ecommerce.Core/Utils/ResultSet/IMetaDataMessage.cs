namespace ecommerce.Core.Utils.ResultSet {
    public interface IMetaDataMessage : IHaveDataObject {
        string Message { get; }
        object DataObject { get; }
    }
}

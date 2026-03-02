namespace ecommerce.Core.Utils.ResultSet {
    public class Metadata : IMetaDataMessage {
        private readonly OperationResult _source;

        public Metadata() {
            Type = MetaDataType.Info;
        }

        public Metadata(OperationResult source, string message) : this() {
            _source = source;
            Message = message;
        }

        public Metadata(OperationResult source, string message, MetaDataType type = MetaDataType.Info) {
            Type = type;
            _source = source;
            Message = message;
        }

        public string Message { get; init; }
        public MetaDataType Type { get; init; }
        public bool IsSystemError { get { return Type == MetaDataType.SystemError; } }
        public object DataObject { get; private set; }
        public void AddData(object data) {
            if (data is Exception exception && _source.Metadata == null)
            {
                _source.Metadata = new Metadata(_source, exception.Message);
            }
            else
            {
                _source.Metadata.DataObject = data;
            }
        }
    }
}

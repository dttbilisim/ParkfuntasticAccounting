using System.Text;

namespace ecommerce.Core.Utils.ResultSet {
    public static class OperationResultExtensions {
        /// <summary>
        /// Create or Replace special type of metadata
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        public static IHaveDataObject? AddInfo(this OperationResult source, string message) {
            source.Metadata = new Metadata(source, message);
            return source.Metadata;
        }

        /// <summary>
        /// Create or Replace special type of metadata
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        public static IHaveDataObject? AddSuccess(this OperationResult source, string message) {
            source.Metadata = new Metadata(source, message, MetaDataType.Success);
            return source.Metadata;
        }

        /// <summary>
        /// Create or Replace special type of metadata
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        public static IHaveDataObject? AddWarning(this OperationResult source, string message) {
            source.Metadata = new Metadata(source, message, MetaDataType.Warning);
            return source.Metadata;
        }

        /// <summary>
        /// Create or Replace special type of metadata
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        public static IHaveDataObject? AddError(this OperationResult source, string message) {
            source.Metadata = new Metadata(source, message, MetaDataType.Error);
            return source.Metadata;
        }

        public static IHaveDataObject? AddSystemError(this OperationResult source, string message) {
            source.Metadata = new Metadata(source, message, MetaDataType.SystemError);
            return source.Metadata;
        }

        /// <summary>
        /// Create or Replace special type of metadata
        /// </summary>
        /// <param name="source"></param>
        /// <param name="exception"></param>
        public static IHaveDataObject? AddError(this OperationResult source, Exception exception) {
            source.Exception = exception;
            source.Metadata = new Metadata(source, exception?.Message, MetaDataType.Error);
            if (exception != null)
            {
            }
            return source.Metadata;
        }

        /// <summary>
        /// Create or Replace special type of metadata
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public static IHaveDataObject? AddError(this OperationResult source, string message, Exception exception) {
            source.Exception = exception;
            source.Metadata = new Metadata(source, message, MetaDataType.Error);
            return source.Metadata;
        }

        /// <summary>
        /// Gather information from result metadata
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetMetadataMessages(this OperationResult source) {
            if (source == null) throw new ArgumentNullException();

            var sb = new StringBuilder();
            if (source.Metadata != null)
            {
                sb.AppendLine($"{source.Metadata.Message}");
            }
            return sb.ToString();

        }
    }
}

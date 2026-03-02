using System.Diagnostics.CodeAnalysis;
namespace ecommerce.Core.Utils.ResultSet {
    public abstract class OperationResult {
        public Metadata? Metadata { get; set; }
        public Exception? Exception { get; set; }

        public static IActionResult<TResult> CreateResult<TResult>(TResult result, Exception? exception = null) {
            var operation = new IActionResult<TResult>
            {
                Result = result,
                Exception = exception
            };
            return operation;
        }

        public static IActionResult<TResult> CreateResult<TResult>() {
            return CreateResult(default(TResult)!);
        }
    }
    [Serializable]
    public class IActionResult<T> : OperationResult {
        /// </summary>
        public T Result { get; set; }
       

        [MemberNotNullWhen(true, nameof(Result))]
        public bool Ok
        {
            get
            {
                if (Metadata == null)
                {
                    return Exception == null && Result != null;
                }
                return Exception == null
                       && Result != null
                       && Metadata?.Type != MetaDataType.Error;
            }
        }

         
    }
}

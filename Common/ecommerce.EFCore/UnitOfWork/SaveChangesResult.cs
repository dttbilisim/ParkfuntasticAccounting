using System.Diagnostics.CodeAnalysis;

namespace ecommerce.Admin.EFCore.UnitOfWork;

public class SaveChangesResult
{
    public SaveChangesResult() => Messages = new List<string>();

    public SaveChangesResult(string message) : this() => AddMessage(message);

    public Exception? Exception { get; set; }

    [MemberNotNullWhen(false, nameof(Exception))]
    public bool IsOk => Exception == null;

    public void AddMessage(string message) => Messages.Add(message);

    private List<string> Messages { get; }
}
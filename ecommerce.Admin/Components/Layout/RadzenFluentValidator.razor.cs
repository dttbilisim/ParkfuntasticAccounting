using System.Collections;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;

namespace ecommerce.Admin.Components.Layout;

public partial class RadzenFluentValidator<TItem> : RadzenComponent
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public bool Popup { get; set; }

    [Parameter]
    public bool ShowSummary { get; set; }

    [Parameter]
    public IValidator<TItem>? Validator { get; set; }

    public bool IsValid { get; protected set; } = true;

    [CascadingParameter]
    public EditContext? EditContext { get; set; }

    private IDictionary<FieldIdentifier, (string PropertyName, string Message)> ValidationMessages { get; set; } = new Dictionary<FieldIdentifier, (string PropertyName, string Message)>();

    private ValidationMessageStore? MessageStore { get; set; }

    private FieldIdentifier? ValidatedFieldIdentifier { get; set; }

    private string? Text { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);

        var isVisible = Visible && EditContext != null;

        if (!isVisible)
        {
            RemoveFromEditContext();
            return;
        }

        if (MessageStore == null)
        {
            MessageStore = new ValidationMessageStore(EditContext!);

            EditContext!.OnFieldChanged += ValidateField;
            EditContext.OnValidationRequested += ValidateModel;
            EditContext.OnValidationStateChanged += ValidationStateChanged;
        }
    }

    public string? GetValidationMessage<TField>(Expression<Func<TItem, TField>> accessor)
    {
        if (accessor.Body is not MemberExpression memberExpression)
        {
            return null;
        }

        var propertyName = memberExpression.Member.Name;

        var model = EditContext!.Model;

        var childExpression = memberExpression.Expression;

        while (childExpression is MemberExpression childMemberExpression)
        {
            model = Expression.Lambda(childExpression, accessor.Parameters).Compile().DynamicInvoke(model) ?? model;
            childExpression = childMemberExpression.Expression;
        }

        return ValidationMessages.TryGetValue(new FieldIdentifier(model, propertyName), out var message) ? message.Message : null;
    }

    public string? GetValidationMessage(string propertyName)
    {
        return ValidationMessages.TryGetValue(EditContext!.Field(propertyName), out var message) ? message.Message : null;
    }

    public IDictionary<string, string> GetValidationMessages()
    {
        var validationMessages = new Dictionary<string, string>();

        foreach (var (_, value) in ValidationMessages)
        {
            validationMessages[value.PropertyName] = value.Message;
        }

        return validationMessages;
    }

    private void RemoveFromEditContext()
    {
        if (EditContext != null && MessageStore != null)
        {
            EditContext.OnFieldChanged -= ValidateField;
            EditContext.OnValidationRequested -= ValidateModel;
            EditContext.OnValidationStateChanged -= ValidationStateChanged;

            if (ValidatedFieldIdentifier != null)
            {
                MessageStore.Clear(ValidatedFieldIdentifier.Value);
                ValidationMessages.Remove(ValidatedFieldIdentifier.Value);
            }
        }

        MessageStore = null;
        ValidationMessages.Clear();
        IsValid = true;
    }

    private void ValidateField(object? sender, FieldChangedEventArgs args)
    {
        if (!string.IsNullOrEmpty(Name) && args.FieldIdentifier.FieldName != Name)
        {
            return;
        }

        ValidatedFieldIdentifier = args.FieldIdentifier;

        ValidationResult result;

        if (Validator != null && args.FieldIdentifier.Model == EditContext!.Model)
        {
            result = Validator.Validate((TItem) EditContext.Model, x => x.IncludeProperties(args.FieldIdentifier.FieldName));
        }
        else
        {
            var modelValidator = GetValidatorForModel(args.FieldIdentifier.Model);

            result = modelValidator != null
                ? modelValidator.Validate(
                    new ValidationContext<object>(args.FieldIdentifier.Model, new PropertyChain(), new MemberNameValidatorSelector(new[] { args.FieldIdentifier.FieldName }))
                )
                : new ValidationResult();
        }

        IsValid = result.IsValid;

        Text = null;
        MessageStore!.Clear(args.FieldIdentifier);
        ValidationMessages.Remove(args.FieldIdentifier);

        var error = result.Errors.FirstOrDefault();

        if (error != null)
        {
            Text = error.ErrorMessage;
            MessageStore.Add(args.FieldIdentifier, Text);
            ValidationMessages[args.FieldIdentifier] = (error.FormattedMessagePlaceholderValues?.GetValueOrDefault("PropertyName")?.ToString() ?? error.PropertyName, error.ErrorMessage);
        }

        EditContext?.NotifyValidationStateChanged();
    }

    private void ValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            var fieldIdentifier = EditContext!.Field(Name);
            ValidateField(sender, new FieldChangedEventArgs(fieldIdentifier));
            return;
        }

        ValidatedFieldIdentifier = null;

        ValidationResult result;

        if (Validator != null)
        {
            result = Validator.Validate((TItem) EditContext!.Model);
        }
        else
        {
            var modelValidator = GetValidatorForModel(EditContext!.Model);

            result = modelValidator != null
                ? modelValidator.Validate(new ValidationContext<object>(EditContext.Model))
                : new ValidationResult();
        }

        IsValid = result.IsValid;

        MessageStore!.Clear();
        ValidationMessages.Clear();

        foreach (var error in result.Errors)
        {
            var propertyName = error.PropertyName;
            var model = EditContext.Model;

            var field = GetFieldIdentifierByPath(model, propertyName);

            MessageStore.Add(field, error.ErrorMessage);
            ValidationMessages[field] = (error.FormattedMessagePlaceholderValues?.GetValueOrDefault("PropertyName")?.ToString() ?? error.PropertyName, error.ErrorMessage);
        }

        EditContext?.NotifyValidationStateChanged();
    }

    private FieldIdentifier GetFieldIdentifierByPath(object value, string path)
    {
        var fieldModel = value;
        var fieldName = path;

        var currentType = value.GetType();
        var currentObj = value;

        foreach (var propertyName in path.Split('.'))
        {
            var brackStart = propertyName.IndexOf("[", StringComparison.Ordinal);
            var brackEnd = propertyName.IndexOf("]", StringComparison.Ordinal);

            var property = currentType.GetProperty(brackStart > 0 ? propertyName[..brackStart] : propertyName);

            if (property == null)
            {
                break;
            }

            fieldName = property.Name;
            fieldModel = currentObj;

            currentObj = property.GetValue(currentObj, null);

            if (brackStart > 0)
            {
                var index = propertyName.Substring(brackStart + 1, brackEnd - brackStart - 1);

                currentObj = currentObj switch
                {
                    IList list => list[int.Parse(index)],
                    IDictionary dictionary => dictionary[index],
                    _ => currentObj
                };
            }

            var objType = currentObj?.GetType();

            if (objType == null)
            {
                break;
            }

            currentType = objType;
        }

        return new FieldIdentifier(fieldModel!, fieldName);
    }

    private IValidator? GetValidatorForModel(object model)
    {
        IValidator? service = null;

        var serviceType = typeof(IValidator<>).MakeGenericType(model.GetType());
        try
        {
            service = ServiceProvider.GetService(serviceType) as IValidator;
        }
        catch
        {
            // ignored
        }

        return service;
    }

    public string GetErrorCssClass()
    {
        return "rz-message rz-messages-error rz-display-block rz-m-0";
    }

    protected override string GetComponentCssClass()
    {
        return $"rz-message rz-messages-error {(Popup ? "rz-message-popup" : "")}";
    }

    private void ValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        StateHasChanged();
    }
}
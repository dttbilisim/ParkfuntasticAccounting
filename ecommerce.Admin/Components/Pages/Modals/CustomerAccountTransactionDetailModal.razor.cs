using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using Microsoft.AspNetCore.Components;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class CustomerAccountTransactionDetailModal
    {
        [Parameter]
        public CustomerAccountTransactionListDto? Transaction { get; set; }
    }
}

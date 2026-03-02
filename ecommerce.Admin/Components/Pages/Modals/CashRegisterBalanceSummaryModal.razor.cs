using ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto;
using Microsoft.AspNetCore.Components;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class CashRegisterBalanceSummaryModal
    {
        [Parameter]
        public List<CashRegisterBalanceSummaryDto>? BalanceSummaries { get; set; }

        [Parameter]
        public DateTime? StartDate { get; set; }

        [Parameter]
        public DateTime? EndDate { get; set; }
    }
}

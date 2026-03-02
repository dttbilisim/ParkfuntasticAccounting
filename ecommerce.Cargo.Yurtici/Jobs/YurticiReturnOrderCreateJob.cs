using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Cargo.Yurtici.Jobs;

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(YurticiReturnOrderCreateJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class YurticiReturnOrderCreateJob : IAsyncBackgroundJob<YurticiReturnOrderCreateJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly YurticiClient _yurticiClient;

    private readonly IRepository<Orders> _orderRepository;

    public YurticiReturnOrderCreateJob(IUnitOfWork<ApplicationDbContext> context, YurticiClient yurticiClient)
    {
        _context = context;
        _yurticiClient = yurticiClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(YurticiReturnOrderCreateJobArgs args)
    {
        var order = await _orderRepository.GetAll(false)
         
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Include(o => o.Cargo)
            .Where(
                o => o.Id == args.OrderId
                     && o.ReturnCargoExternalId == null
                     && o.ProblemStatus == OrderProblemStatus.CreatingReturnShipment
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("yurtiçi")
            )
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return;
        }

      //  var returnCode = "ECZ-" + string.Join('-', GenerateReturnCode(order.Id).Chunk(4));

        var startDate = DateTime.Now.Date;
        var endDate = startDate.AddDays(4);

        var yurticiRequest = new cancelNgiShipment()
        {
            ngiDocumentKey = order.OrderNumber,
            ngiCargoKey = order.OrderNumber,
            cancellationDescription = order.OrderNumber
            
        };

        var yurticiResponse = await _yurticiClient.CreateReturnOrderAsync(yurticiRequest);

        if (yurticiResponse.errCode != "0")
        {
            throw new Exception("Could not create yurtici return delivery: " + yurticiResponse.outResult ?? yurticiResponse.errCode);
        }

        order.ReturnCargoExternalId = order.OrderNumber;
        order.ProblemStatus = OrderProblemStatus.WaitingCargoShipment;

        _orderRepository.Update(order);

        await _context.SaveChangesAsync();
    }

    private string GenerateReturnCode(int orderId)
    {
        var random = new Random();

        while (true)
        {
            var orderNumber = orderId.ToString();

            while (orderNumber.Length < 8)
            {
                orderNumber += random.Next(0, 9);
            }

            if (orderNumber.Length > 8)
            {
                orderNumber = orderNumber[..8];
            }

            return orderNumber;
        }
    }
}
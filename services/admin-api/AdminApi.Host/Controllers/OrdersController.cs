using AdminApi.Host.Models;
using AdminApi.Host.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController(AdminDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        var orderEntity = new OrderEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ProductCode = request.ProductCode,
            Amount = request.Amount,
            Status = "Pending",
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Orders.Add(orderEntity);
        await dbContext.SaveChangesAsync();

        var order = new OrderDto(
            orderEntity.Id,
            orderEntity.UserId,
            orderEntity.ProductCode,
            orderEntity.Amount,
            orderEntity.Status,
            orderEntity.UpdatedAt
        );

        return Ok(order);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(new OrderDto(
            order.Id,
            order.UserId,
            order.ProductCode,
            order.Amount,
            order.Status,
            order.UpdatedAt
        ));
    }
}

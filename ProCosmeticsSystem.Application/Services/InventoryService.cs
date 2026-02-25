using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Application.Services;

public class InventoryService
{
    private readonly IInventoryRepository _repo;

    public InventoryService(IInventoryRepository repo)
    {
        _repo = repo;
    }

    public Task<List<InventoryDto>> GetAllAsync() => _repo.GetAllAsync();

    public Task<List<InventoryDto>> GetLowStockAsync() => _repo.GetLowStockAsync();

    public async Task AdjustAsync(AdjustInventoryRequest request)
    {
        if (request.Quantity == 0)
            throw new ValidationException("Quantity", "Quantity cannot be zero.");

        var inventory = await _repo.GetByProductIdAsync(request.ProductId)
            ?? throw new NotFoundException("Inventory", request.ProductId);

        if (inventory.QuantityOnHand + request.Quantity < 0)
            throw new ValidationException("Quantity", "Insufficient stock for this adjustment.");

        await _repo.UpdateQuantityAsync(request.ProductId, request.Quantity);
        await _repo.AddTransactionAsync(new InventoryTransaction
        {
            ProductId = request.ProductId,
            TransactionType = InventoryTransactionType.Adjustment,
            Quantity = request.Quantity,
            Notes = request.Notes
        });
    }
}

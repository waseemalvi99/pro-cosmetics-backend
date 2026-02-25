using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Deliveries;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Application.Services;

public class DeliveryService
{
    private readonly IDeliveryRepository _repo;
    private readonly IDeliveryManRepository _deliveryManRepo;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public DeliveryService(
        IDeliveryRepository repo,
        IDeliveryManRepository deliveryManRepo,
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _deliveryManRepo = deliveryManRepo;
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public Task<PagedResult<DeliveryDto>> GetAllAsync(int page, int pageSize, int? deliveryManId, string? status)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        return _repo.GetAllAsync(page, pageSize, deliveryManId, status);
    }

    public async Task<DeliveryDto> GetByIdAsync(int id) =>
        await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Delivery", id);

    public async Task<int> CreateAsync(CreateDeliveryRequest request)
    {
        var delivery = new Delivery
        {
            SaleId = request.SaleId,
            DeliveryManId = request.DeliveryManId,
            DeliveryAddress = request.DeliveryAddress,
            Notes = request.Notes,
            Status = request.DeliveryManId.HasValue ? DeliveryStatus.Assigned : DeliveryStatus.Pending,
            AssignedAt = request.DeliveryManId.HasValue ? DateTime.UtcNow : null,
            CreatedBy = _currentUser.UserId
        };

        var id = await _repo.CreateAsync(delivery);

        if (_currentUser.UserId.HasValue)
            await _notificationService.SendAsync(_currentUser.UserId.Value, "Delivery Created", $"Delivery #{id} has been created.");

        return id;
    }

    public async Task PickupAsync(int id, UpdateDeliveryStatusRequest request)
    {
        var delivery = await GetDeliveryEntity(id);
        if (delivery.Status != nameof(DeliveryStatus.Assigned))
            throw new AppException("Only assigned deliveries can be picked up.");

        var entity = new Delivery
        {
            Id = id,
            SaleId = delivery.SaleId,
            DeliveryManId = delivery.DeliveryManId,
            Status = DeliveryStatus.PickedUp,
            PickedUpAt = DateTime.UtcNow,
            AssignedAt = delivery.AssignedAt,
            DeliveryAddress = delivery.DeliveryAddress,
            Notes = request.Notes ?? delivery.Notes,
            UpdatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdateAsync(entity);

        if (_currentUser.UserId.HasValue)
            await _notificationService.SendAsync(_currentUser.UserId.Value, "Delivery Picked Up", $"Delivery #{id} has been picked up.");
    }

    public async Task DeliverAsync(int id, UpdateDeliveryStatusRequest request)
    {
        var delivery = await GetDeliveryEntity(id);
        if (delivery.Status != nameof(DeliveryStatus.PickedUp) && delivery.Status != nameof(DeliveryStatus.InTransit))
            throw new AppException("Only picked up or in-transit deliveries can be marked as delivered.");

        var entity = new Delivery
        {
            Id = id,
            SaleId = delivery.SaleId,
            DeliveryManId = delivery.DeliveryManId,
            Status = DeliveryStatus.Delivered,
            AssignedAt = delivery.AssignedAt,
            PickedUpAt = delivery.PickedUpAt,
            DeliveredAt = DateTime.UtcNow,
            DeliveryAddress = delivery.DeliveryAddress,
            Notes = request.Notes ?? delivery.Notes,
            UpdatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.UpdateAsync(entity);

        if (_currentUser.UserId.HasValue)
            await _notificationService.SendAsync(_currentUser.UserId.Value, "Delivery Completed", $"Delivery #{id} has been delivered.");
    }

    private async Task<DeliveryDto> GetDeliveryEntity(int id) =>
        await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Delivery", id);
}

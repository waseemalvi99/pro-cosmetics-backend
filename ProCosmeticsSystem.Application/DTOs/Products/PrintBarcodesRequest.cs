namespace ProCosmeticsSystem.Application.DTOs.Products;

public class PrintBarcodesRequest
{
    public List<int> ProductIds { get; set; } = new();
}

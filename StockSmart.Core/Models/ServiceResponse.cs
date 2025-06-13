namespace StockSmart.Core.Models;

public class ServiceResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; }
    public T Data { get; set; }
}
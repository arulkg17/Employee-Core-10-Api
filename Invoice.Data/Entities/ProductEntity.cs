using System.ComponentModel.DataAnnotations;

namespace Invoice.Data.Entities;

public class ProductEntity
{
    [Key]
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
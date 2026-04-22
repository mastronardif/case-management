using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class InventoryItem
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public int Quantity { get; set; }

    [Timestamp] // Enables optimistic concurrency checks
    public byte[] RowVersion { get; set; }
}

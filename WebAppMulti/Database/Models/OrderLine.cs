public class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; }

    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; }

    public int Quantity { get; set; }
}
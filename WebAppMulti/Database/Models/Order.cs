public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }

    // Foreign keys
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}
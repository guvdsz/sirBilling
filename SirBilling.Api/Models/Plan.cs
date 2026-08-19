public class Plan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Subscription> Subscriptions { get; set; }
        = new List<Subscription>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
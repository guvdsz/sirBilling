public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
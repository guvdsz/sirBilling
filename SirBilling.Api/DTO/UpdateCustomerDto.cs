using System.ComponentModel.DataAnnotations;

public class UpdateCustomerDto
{
    [StringLength(100, MinimumLength = 3)]
    public string? Name { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
}
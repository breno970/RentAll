using Microsoft.AspNetCore.Identity;

namespace aluguel.Areas.Identity.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    // Null for legacy accounts with no reliable registration timestamp.
    public DateTime? CreatedAt { get; set; }
}


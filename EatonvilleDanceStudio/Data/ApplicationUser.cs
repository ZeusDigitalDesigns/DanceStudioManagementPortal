using Microsoft.AspNetCore.Identity;
using ServiceStack.DataAnnotations;

namespace EatonvilleDanceStudio.Data;

/// <summary>
/// Represents the user entity in the Eatonville Dance Studio application,
/// extending the IdentityUser to include additional attributes for managing user-specific data.
/// </summary>
/// <remarks>
/// The ApplicationUser class encapsulates information about users,
/// including their identity information and additional profile details
/// such as FirstName, LastName, and AccountStatus.
/// </remarks>
public class ApplicationUser : IdentityUser
{
    [StringLength(50)]
    public string FirstName { get; set; }
    
    [StringLength(50)]
    public string LastName { get; set; }

    [StringLength(10)] public string AccountStatus { get; set; } = "Approved";
}

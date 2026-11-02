using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace EatonvilleDanceStudio.ViewModels
{
    public class AccountManageViewModel
    {
        [BindProperty]
        public string? Username { get; set; }

        [BindProperty]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        [BindProperty]
        public string? PhoneNumber { get; set; }
    }
}
using EatonvilleDanceStudio.Models;
using System.Collections.Generic;

namespace EatonvilleDanceStudio.ViewModels;

public class ParentClassBoardViewModel
{
    
    // The specific class we are viewing the board for
    public required DanceClass DanceClass { get; set; }
    
    // A list of all previously posted announcements for this class
    public List<ClassAnnouncement> Announcements { get; set; } = new List<ClassAnnouncement>();
}
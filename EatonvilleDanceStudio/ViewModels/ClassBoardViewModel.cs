using EatonvilleDanceStudio.Models;
using System.Collections.Generic;

namespace EatonvilleDanceStudio.ViewModels;

public class ClassBoardViewModel
{
    // The specific class we are viewing the board for
    public required DanceClass DanceClass { get; set; }
    
    // A list of all previously posted announcements for this dance class
    public List<ClassAnnouncement> Announcements { get; set; } = new List<ClassAnnouncement>();
    
    // A blank slate for the instructor to post a new announcement
    public ClassAnnouncement NewAnnouncement { get; set; } = new ClassAnnouncement();
}
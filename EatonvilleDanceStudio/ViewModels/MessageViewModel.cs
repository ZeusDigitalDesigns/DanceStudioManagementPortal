using System.Collections.Generic;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.ViewModels
{
    public class MessagesViewModel
    {
        public List<StudioMessage> Inbox { get; set; } = new List<StudioMessage>();
        
        public List<StudioMessage> Sent { get; set; } = new List<StudioMessage>();
    }
}
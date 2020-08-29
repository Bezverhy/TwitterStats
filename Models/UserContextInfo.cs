using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Twitter_stats.Models
{
    public class UserContextInfo
    {
        public string MentionedUser { get; set; }
        public int MentionsNumber { get; set; }
        public string UserImgLink { get; set; }
    }

}

using LinqToTwitter;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Twitter_stats.Models
{
    public class TUserData
    {
        public string Username { get; set; }

        public string Statuses { get; set; }

        public string Timespan { get; set; }

        public List<string> Date { get; set; }

        public  List<UserMentionEntity> UserMentions { get; set; }

        public  List<HashTagEntity> HashTags { get; set; }

        public Dictionary<string, int> HashTagDictionary { get; set; }

        public Dictionary<string, int> MentionedUsersDictionary { get; set; }

        public Dictionary<string, int> UserTopicsDictionary { get; set; }

        public Dictionary<string, int> TweetCreationTime { get; set; }

        public void FillDictionary(Dictionary<String,int> Dict,string word)
        {
            if (!Dict.ContainsKey(word))
            {
                // add new word to collection
                Dict.Add(word, 1);
            }
            else
            {
                // update word occurrence count
                Dict[word] += 1;
            }
        }

        public void AddTimespan()
        {
            for (int i = 0; i < 25; i++)
            {
                if (i < 10) this.TweetCreationTime.Add("0" + i.ToString(), 0);

                else this.TweetCreationTime.Add(i.ToString(), 0);
            }
        }

    }
}

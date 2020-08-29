using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LinqToTwitter;
using Twitter_stats.Models;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace Twitter_stats.Controllers
{
    public class HomeController : Controller
    {

        //Twitter API access tokens and keys
        public readonly SingleUserAuthorizer auth = new SingleUserAuthorizer()
        {
            CredentialStore = new SingleUserInMemoryCredentialStore
            {
                ConsumerKey = "KfTrCw2GFEFSbqyNe810IUFoP",
                ConsumerSecret = "sB74ZGj7T6Hy42LiiBPOo8YHT8gW01LPv2s8JPGeRomtKzr7O2",
                AccessToken = "1255976389603397638-emjulzzVHTdmcBU3sLV9JZGnTxjLVo",
                AccessTokenSecret = "Zgqnv5ufgTcVkIeetRTW187HeiYvj3F0iztngQrhVsXqY"
            }
        };

        public readonly int Count = 100;
        public readonly byte minWordLength = 2;
        public readonly char[] delimiterChars = { ' ', '.', ',', ';', ':', '?', '\n', '\r', '@', ':', '$' };

        public IActionResult Index()
        {
            return View();
        }

        [ActionName("About")]
        public async Task<ActionResult> AnalyzeTwitterContextAsync()
        {
            var twitterCtx = new TwitterContext(auth);

            TUserData UserData = new TUserData()
            {
                Username = HttpContext.Request.Query["AccountName"],
                UserMentions = new List<UserMentionEntity>(),
                HashTags = new List<HashTagEntity>(),
                MentionedUsersDictionary = new Dictionary<string, int>(),
                HashTagDictionary = new Dictionary<string, int>(),
                UserTopicsDictionary = new Dictionary<string, int>(),
                Date = new List<string>(),
                TweetCreationTime = new Dictionary<string, int>()
            };

            List<UserContextInfo> UserImg = new List<UserContextInfo>();

            string[] BAD_WORDS = { "\n", "//t", "who", "and", "the", "http", "https", "ftp", "when",
                "were", "them", "they", "we", "was", "what","that","were", "but", "which","for","!","you",":",
                "are", "this","has"," &amp","...",UserData.Username.Trim().ToLower()};

            int RetweetCount = 0, UrlCount = 0, RepliesCount = 0,
            MediaCount = 0, MentionsCount = 0, HashtagCount = 0;
            string Urls = "";

            try
            {
                //Getting tweets using Linq2Twitter library
                var tweets =
               await
               (from tweet in twitterCtx.Status
                where tweet.Type == StatusType.User &&
                      tweet.ScreenName == UserData.Username
                      && tweet.Count == Count
                      && tweet.IncludeEntities == true
                      && tweet.TweetMode == TweetMode.Extended
                select tweet)
               .ToListAsync();

                //Getting user information using Linq2Twitter library
                var user =
             await
             (from tweet in twitterCtx.User
              where tweet.Type == UserType.Show &&
                    tweet.ScreenName == UserData.Username &&
                    tweet.IncludeEntities == true &&
                    tweet.TweetMode == TweetMode.Extended
              select tweet)
             .SingleOrDefaultAsync();


                tweets.ForEach(tweet =>
                    {
                        if (tweet != null)
                        {

                            foreach (var BAD_WORD in BAD_WORDS) { tweet.FullText = tweet.FullText.Trim().ToLower().Replace(BAD_WORD, ""); }

                            UserData.Statuses += tweet.FullText;

                            UserData.UserMentions.AddRange(tweet.Entities.UserMentionEntities);
                            UserData.HashTags.AddRange(tweet.Entities.HashTagEntities);
                        }
                    });

                string[] words = UserData.Statuses.Split(delimiterChars);

                foreach (string word in words)
                {
                    string w = word.Trim().ToLower();
                    if (w.Length > minWordLength)
                    {
                        UserData.FillDictionary(UserData.UserTopicsDictionary, w);
                    }
                }

                UserData.HashTags.ForEach(hashtag =>
                    {
                        string tag = hashtag.Text;
                        UserData.FillDictionary(UserData.HashTagDictionary, tag);
                    });

                UserData.UserMentions.ForEach(mentions =>
                    {
                        string mention = mentions.ScreenName;

                        if (mentions.ScreenName != user.ScreenNameResponse)
                            UserData.FillDictionary(UserData.MentionedUsersDictionary, mention);
                    });

                foreach (var User in UserData.MentionedUsersDictionary.OrderByDescending(key => key.Value))
                {
                    try
                    {
                        // Getting image links using Linq2Twitter query
                        var Image =
                 await
                 (from tweet in twitterCtx.User
                  where tweet.Type == UserType.Show &&
                        tweet.ScreenName == User.Key
                  select tweet).
                 SingleOrDefaultAsync();

                        UserImg.Add(new UserContextInfo
                        {
                            MentionedUser = User.Key,
                            MentionsNumber = User.Value,
                            UserImgLink = Image.ProfileImageUrl

                        });
                    }
                    catch { }
                }

                tweets.ForEach(hours =>
                {
                    UserData.Date.Add(hours.CreatedAt.ToString("HH"));
                });

                //Find unique domains in user tweets
                tweets.ForEach(tweet =>
                {
                    tweet.Entities.UrlEntities.ForEach(url =>
                    {
                        var position = url.DisplayUrl.IndexOf("/");
                        try
                        {
                            Urls += url.DisplayUrl.Substring(0, position) + " ";
                        }
                        catch
                        {
                            Urls += url.DisplayUrl + " ";
                        }
                    });
                });

                string[] Domains = Urls.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                var domains_query =
                    (from string domain in Domains
                     orderby domain
                     select domain).Distinct();

                string[] UniqueDomains = domains_query.ToArray();

                //Count number of tweets which have tags, replies etc.
                tweets.ForEach(tweet =>
                {

                    if (tweet.Retweeted) RetweetCount++;

                    if (tweet.InReplyToScreenName != null) RepliesCount++;

                    if (tweet.Entities.MediaEntities.Count > 0) MediaCount++;

                    if (tweet.Entities.UrlEntities.Count > 0) UrlCount++;

                    if (tweet.Entities.HashTagEntities.Count > 0) HashtagCount++;

                    if (tweet.Entities.UserMentionEntities.Count > 0) MentionsCount++;
                });

                //Add timespan (00, 01,02 etc) into dictionary
                UserData.AddTimespan();

                UserData.Date.ForEach(hours =>
                        {
                            if (UserData.TweetCreationTime.ContainsKey(hours))
                                UserData.TweetCreationTime[hours] += 1;
                        });

                foreach (var hours in UserData.TweetCreationTime)
                {
                    UserData.Timespan += hours.Value.ToString() + ",";
                }

                var lastStatus =
                        user.Status == null ? "No Status" : user.Status.FullText;

                var verified =
                  user.Verified == false ? "Not verified" : "Verified";
                if (user.Protected) verified = "Protected";

                //User Information 
                ViewData["Username"] = UserData.Username;
                ViewData["SmallName"] = "@" + user.ScreenNameResponse;
                ViewData["Image"] = user.ProfileImageUrl.Replace("normal", "bigger");
                ViewData["Name"] = user.Name; ViewData["Verified"] = verified;
                ViewData["JoinedOn"] = user.CreatedAt;
                ViewData["Location"] = user.Location ?? "Не вказано";
                ViewData["Timezone"] = user.TimeZone ?? "Не вказано";
                ViewData["Language"] = user.Lang ?? "Не вказано";
                ViewData["Bio"] = user.Description;
                ViewData["Url"] = user.Url;

                //Statistics
                ViewData["TweetCount"] = user.StatusesCount;
                ViewData["Followers"] = user.FollowersCount;
                ViewData["Following"] = user.FriendsCount;
                ViewData["Listed"] = user.ListedCount;
                ViewData["UserId"] = user.UserIDResponse;
                ViewData["FollowingRatio"] = user.FollowersCount / user.FriendsCount;
                ViewData["LastStatus"] = Regex.Replace(lastStatus, @"((http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*)", @"<a href='$1'>$1</a>");


                //Tweets context
                ViewData["Retweets"] = RetweetCount;
                ViewData["Replies"] = RepliesCount;
                ViewData["MediaCount"] = MediaCount;
                ViewData["UrlCount"] = UrlCount;
                ViewData["HashTagCount"] = HashtagCount;
                ViewData["UserMentionsCount"] = MentionsCount;
                ViewData["Domains"] = UniqueDomains;

                //Mentions, topics, hashtags
                ViewData["PopularTopics"] = UserData.UserTopicsDictionary;
                ViewData["UserPictures"] = UserImg;
                ViewData["Hashtags"] = UserData.HashTagDictionary;

                //Tweets timespans
                ViewData["TweetTimespan"] = UserData.Timespan;
                return View("About");
            }

            catch
            {
                ViewData["Error"] = "Вибачте, неможливо обробити ваш запит!";
                return View("Error");
            }

        }
    }
}
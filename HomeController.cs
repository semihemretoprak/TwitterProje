using Proje.Model.Entities;
using Proje.Service.Option;
using Proje.WebUI.Areas.Administrator.Models;
using Proje.WebUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proje.WebUI.Controllers
{
    [AdminAuthentication]
    public class HomeController : Controller
    {
        #region Instance field
        AppUserService aus = new AppUserService();
        GroupUserService gus = new GroupUserService();
        GroupService gs = new GroupService();
        TweetService ts = new TweetService();
        HashtagService hs = new HashtagService();
        VideoService vs = new VideoService();
        PhotoService ps = new PhotoService();
        MessageService ms = new MessageService();
        FollowerService fs = new FollowerService();
        CountryService counts = new CountryService();
        CityService cits = new CityService();
        BlockedFollowerService bs = new BlockedFollowerService();
        CommentService cs = new CommentService();
        ActivityService acs = new ActivityService();
        ActivityGoingUserService agus = new ActivityGoingUserService();
        ActivityThinkingUserService atus = new ActivityThinkingUserService();
        LikeService ls = new LikeService();
        Encryption enc = new Encryption(); 
        #endregion
        public ActionResult HomePage()
        {
            return View(new Tweet());
        }
        [HttpPost]
        public ActionResult HomePage(Tweet tweet, Hashtag hashtag, HttpPostedFileBase[] PhotoVideo, Photo photo, Video video)//Tweet ve Hashtag Ekleme
        {
            AppUser gelen = (AppUser)Session["oturum"];
            if (Session["oturum"] != null)
            {
                gelen = (AppUser)Session["oturum"];
                tweet.AppUserID = gelen.ID;
            }
            if (ModelState.IsValid)
            {
                tweet.Like = 0;
                tweet.Dislike = 0;
                tweet.Retweet = 0;
                #region Add Hashtag
                if (tweet.TweetText!=null)
                {
                    if (tweet.TweetText.Contains('#'))
                    {
                        string[] text = tweet.TweetText.Split(' ');
                        foreach (var item in text)
                        {
                            if (hs.Any(x => x.HashTag.Contains(item)))
                            {
                                var hashtagItem = hs.GetByDefault(x => x.HashTag == item);
                                hashtagItem.Quantity += 1;
                                hashtagItem.Tweets.Add(tweet);
                                hs.Update(hashtagItem);
                            }
                            else
                            {
                                if (item.Contains('#'))
                                {
                                    hashtag.HashTag = item;
                                    hashtag.Quantity = 1;
                                    tweet.Hashtags.Add(hashtag);
                                    ts.Add(tweet);
                                }
                            }
                        }
                    }
                    else
                    {
                        ts.Add(tweet);
                    }
                }
                else
                {
                    ViewBag.Message = "Tweet Giriniz.";
                    return View();
                }
                #endregion
                #region Add Photo and Video
                bool isUploadedVideo;
                bool isUploadedPhoto;
                foreach (var itemPhoto in PhotoVideo)
                {
                    if (itemPhoto != null)
                    {
                        if (itemPhoto.ContentType.Contains("image"))
                        {
                            string photoFileResult = FxFunction.Upload(PhotoVideo, FolderPath.TweetPhoto, out isUploadedPhoto);
                            if (isUploadedPhoto)
                            {
                                photo.TweetID = tweet.ID;
                                photo.Description = tweet.TweetText;
                                photo.ImagePath = photoFileResult;
                                ps.Add(photo);
                            }
                        }
                        else if (itemPhoto.ContentType.Contains("video"))
                        {
                            string videoFileResult = FxFunction.Upload(PhotoVideo, FolderPath.TweetMovie, out isUploadedVideo);
                            if (isUploadedVideo)
                            {
                                video.ID = Guid.NewGuid();
                                video.Description = tweet.TweetText;
                                video.VideoPath = videoFileResult;
                                vs.Add(video);
                                tweet.TweetVideoID = video.ID;
                                ts.Update(tweet);
                            }
                        }
                    }
                }
                return RedirectToAction("HomePage");
                #endregion
            }
            else
            {
                ViewBag.Message = "Ekleme sırasında hata oluştu.";
            }
            ViewBag.AppUserID = new SelectList(aus.GetActive(), "ID", "Name", tweet.AppUserID);

            return View();
        }
        public PartialViewResult _HashtagList()
        {
            return PartialView(hs.GetActive().OrderByDescending(p => p.Quantity).Take(5));
        }//Hashtag PartialView
        public PartialViewResult _WhoToFollowList()
        {
            AppUser gelen = (AppUser)Session["oturum"];//Giriş yapan kullanıcı
            var appusers = aus.GetActive().Where(x => x.ID != gelen.ID);//Kullanıcının, engellediği ve takip ettiği kullanıcılar 
            var followers = fs.GetActive().Where(x => x.AppUserID == gelen.ID);
            var blockedUsers = bs.GetActive().Where(x => x.AppUserID == gelen.ID);
            List<AppUser> userlist = new List<AppUser>();
            #region Kullanıcının, engelledikleri ve takip ettikleri dışındaki kullanıcıların listesi
            foreach (AppUser item in appusers)
            {
                var user = item.ID;
                if (blockedUsers.Count() != 0 && followers.Count() == 0)
                {
                    if (!blockedUsers.Any(x => x.DAppUserID == user))
                    {
                        userlist.Add(aus.GetByID(user));
                    }
                }
                else if (followers.Count() != 0 && blockedUsers.Count() != 0)
                {

                    if (!followers.Any(x => x.DAppUserID == user) && !blockedUsers.Any(x => x.DAppUserID == user))
                    {
                        userlist.Add(aus.GetByID(user));
                    }
                }
                else if (followers.Count() != 0 && blockedUsers.Count() == 0)
                {
                    if (!followers.Any(x => x.DAppUserID == user))
                    {
                        userlist.Add(aus.GetByID(user));
                    }
                }
                else
                {
                    userlist.Add(aus.GetByID(user));
                }
            }
            #endregion
            return PartialView(userlist);
        }//Kullanıcıların listesi PartialView
        public PartialViewResult _TweetList()
        {
            AppUser gelen = (AppUser)Session["oturum"];//Giriş yapan kullanıcı
            List<Tweet> tweetList = new List<Tweet>();//Kullanıcı arkadaşlarının tweet,retweet ve bilgilerinin listesi
            var tweets = ts.GetActive().Where(x => x.AppUserID == gelen.ID);//Kullanıcının Tweet,Retweet bilgileri      
            var followers = fs.GetActive().Where(x => x.AppUserID == gelen.ID);
            #region Takip ettiği kullanıcıların Tweet,bilgileri
            foreach (Follower itemFollower in followers)
            {
                var Tweets = ts.GetActive().Where(x => x.AppUserID == itemFollower.DAppUserID);
                foreach (Tweet itemTweet in Tweets)
                {
                    tweetList.Add(ts.GetByID(itemTweet.ID));
                }
            }
            #endregion
            tweetList.AddRange(tweets);
            return PartialView(tweetList.OrderByDescending(x=>x.CreatedDate));
        }//Tweet PartialView
        public PartialViewResult _FollowList()
        {
            AppUser gelen = (AppUser)Session["oturum"];//Giriş yapan kullanıcı
            return PartialView(fs.GetActive().Where(x => x.AppUserID == gelen.ID));
        }//Follow PartialView
        public PartialViewResult _RetweetCreate(Guid id)
        {
            var tweet = ts.GetByID(id);
            var photo = ps.GetActive().Where(x => x.TweetID == id);
            return PartialView(Tuple.Create<Tweet,IEnumerable<Photo>,Video>(tweet,photo, vs.GetByDefault(x => x.ID == tweet.TweetVideoID)));
        }
        public PartialViewResult _FollowMesaage(Guid id)
        {
            AppUser gelen = (AppUser)Session["oturum"];
            var follow = fs.GetByDefault(x => x.DAppUserID == id);
            List<Message> followMessages = ms.GetDefault(x => x.DAppUserID == gelen.ID);
            if (ms.GetActive().Any(x => x.DAppUserID == id))
            {
                var messages = ms.GetDefault(x => x.AppUserID == gelen.ID);
                foreach (Message item in messages)
                {
                    followMessages.Add(item);
                }
            }
            else if (ms.GetActive().Any(x => x.AppUserID == id))
            {
                var messages = ms.GetDefault(x => x.DAppUserID == gelen.ID);
                foreach (Message item in messages)
                {
                    followMessages.Add(item);
                }
            }
            return PartialView(Tuple.Create<Follower, IEnumerable<Message>>(follow, followMessages.OrderBy(x => x.CreatedDate)));
        }
        public ActionResult FollowMessage(Guid id)
        {
            AppUser gelen = (AppUser)Session["oturum"];
            var follow = fs.GetByDefault(x => x.DAppUserID == id);
            List<Message> followMessages = ms.GetDefault(x => x.DAppUserID == gelen.ID&&x.AppUserID==id);
            if (ms.GetActive().Any(x => x.DAppUserID == id))
            {
                var messages = ms.GetDefault(x => x.AppUserID == gelen.ID);
                foreach (Message item in messages)
                {
                    followMessages.Add(item);
                }
            }
            else if (ms.GetActive().Any(x => x.AppUserID == id))
            {
                var messages = ms.GetDefault(x => x.DAppUserID == gelen.ID);
                foreach (Message item in messages)
                {
                    followMessages.Add(item);
                }
            }
            return View(Tuple.Create<Follower, IEnumerable<Message>>(follow,followMessages.OrderBy(x=>x.CreatedDate)));
        }
        public ActionResult TimeLine(Guid id)
        {
            return View(Tuple.Create<IEnumerable<Tweet>,AppUser>(ts.GetActive().Where(x=>x.AppUserID==id),aus.GetByID(id)));
        }
        public ActionResult TimeLineFriend(Guid id)
        {
            return View(Tuple.Create<IEnumerable<Tweet>, AppUser>(ts.GetActive().Where(x => x.AppUserID == id), aus.GetByID(id)));
        }
        public ActionResult Friends(Guid id)
        {
            return View(Tuple.Create<IEnumerable<Follower>,IEnumerable<BlockedFollower>,AppUser>(fs.GetActive().Where(x=>x.AppUserID==id),bs.GetActive().Where(x=>x.AppUserID==id),aus.GetByID(id)));
        }
        public ActionResult TimelineGroup(Guid id)
        {
            return View(Tuple.Create<List<Group>,AppUser>(gs.GetActive(),aus.GetByID(id)));
        }
        public ActionResult GroupInsert(Guid id)
        {
            return View(Tuple.Create<AppUser,Group>(aus.GetByID(id),new Group()));
        }
        [HttpPost]
        public ActionResult GroupInsert([Bind(Prefix ="Item2")]Group group)
        {
            AppUser gelen = (AppUser)Session["oturum"];
            if (ModelState.IsValid)
            {
                var groups = gs.GetAll();
                foreach (Group item in groups)
                {
                    if (item.Name==group.Name)
                    {
                        gs.Update(group);
                        ViewBag.Message = "Grup Güncellendi";
                        return RedirectToAction("TimeLineGroup", new { id = gelen.ID });
                    }
                }
                group.GroupOwner = gelen.UserName;
                gs.Add(group);
                ViewBag.Message = "Grup eklendi";
            }
            return RedirectToAction("TimeLineGroup",new { id=gelen.ID});
        }
        public ActionResult GroupTransactions(Guid Groupid, Guid Appuserid, GroupUser groupUser,bool Join,bool leaveGroup,bool groupDelete)
        {
            if (Join==true)
            {
                var group = gs.GetByID(Groupid);
                var appUser = aus.GetByID(Appuserid);
                groupUser.AppUserID = appUser.ID;
                groupUser.GroupID = group.ID;
                if (!gus.GetDefault(x => x.GroupID == Groupid).Any(p => p.AppUserID == Appuserid))
                {
                    gus.Add(groupUser);
                    return RedirectToAction("TimeLineGroup", new { id = Appuserid });
                }
                else
                {
                    ViewBag.Message = "Zaten Gruptasınız!";
                    return RedirectToAction("TimeLineGroup", new { id = Appuserid });
                }
            }
            else if (leaveGroup==true)
            {
                var userGroup = gus.GetByDefault(x => x.AppUserID == Appuserid);
                gus.Remove(userGroup.ID);
                return RedirectToAction("TimeLineGroup", new { id = Appuserid });
            }
            else if (groupDelete==true)
            {
                gs.Remove(Groupid);
                var groupuser = gus.GetDefault(x => x.GroupID == Groupid);
                foreach (var item in groupuser)
                {
                    gus.Remove(item.ID);
                }
                return RedirectToAction("TimeLineGroup", new { id = Appuserid });
            }
            else
            {
                ViewBag.Message = "İşlem sırasında hata oluştu.";
                return RedirectToAction("TimeLineGroup", new { id = Appuserid });
            }            
        }//Grup sil, kullanıcı ekle ve çıkar işlemleri
        public ActionResult TimelinePhoto(Guid id)
        {
            return View(Tuple.Create<List<Photo>,AppUser>(ps.GetDefault(x=>x.AppUserID==id),aus.GetByID(id)));
        }
        public ActionResult TimelineVideo(Guid id)
        {
            List<Video> videos = new List<Video>();
            var tweets=ts.GetDefault(x => x.AppUserID == id);
            foreach (Tweet tweet in tweets)
            {
                if (tweet.TweetVideoID!=null)
                {
                    var video = vs.GetByID(tweet.Video.ID);
                    videos.Add(video);
                }
            }
            return View(Tuple.Create<List<Video>,AppUser>(videos,aus.GetByID(id)));
        }
        public ActionResult EditPassword(Guid id)
        {
            return View(aus.GetByID(id));
        }
        [HttpPost]
        public ActionResult EditPassword(AppUser item)
        {
            #region HiddenFor
            AppUser tumHali = aus.GetByID(item.ID);
            item.LocationID = tumHali.LocationID;
            item.CreatedDate = tumHali.CreatedDate;
            item.CreatedIP = tumHali.CreatedIP;
            item.Description = tumHali.Description;
            item.BirthDate = tumHali.BirthDate;
            item.EducationStatus = tumHali.EducationStatus;
            item.EmailAddress = tumHali.EmailAddress;
            item.Gender = tumHali.Gender;
            item.Name = tumHali.Name;
            item.SurName = tumHali.SurName;
            item.UserName = tumHali.UserName;
            item.UserImagePath = tumHali.UserImagePath;
            item.UserCoverImagePath = tumHali.UserCoverImagePath;
            item.EducationStatus = tumHali.EducationStatus; 
            #endregion
            AppUser userToCheck = aus.GetByID(item.ID);
            if (enc.ValidatePassword(item.Password, userToCheck.Password))
            {
                if (item.Password == item.NewPassword)
                {
                    ViewBag.Message = "Yeni şifreniz eski şifrenizle aynı olamaz!";
                    return View(aus.GetByID(item.ID));
                }
                else
                {
                    item.Password = item.NewPassword;
                    string hashedPassword = enc.CreateHash(item.Password);
                    item.Password = hashedPassword;
                    tumHali.Password = item.Password;
                    aus.Update(item);
                    ViewBag.Message = "Şifreniz başarıyla değiştirildi.";
                    return View(aus.GetByID(item.ID));
                }
            }
            return View();
        }
        public ActionResult EditProfile(Guid id)
        {
            return View(aus.GetByID(id));
        }
        [HttpPost]
        public ActionResult EditProfile(AppUser item)
        {
            #region HiddenFor
            AppUser tumHali = aus.GetByID(item.ID);
            item.LocationID = tumHali.LocationID;
            item.CreatedDate = tumHali.CreatedDate;
            item.CreatedIP = tumHali.CreatedIP;
            item.UserName = tumHali.UserName;
            item.Gender = tumHali.Gender;
            item.Password = tumHali.Password;
            item.IsAdministrator = tumHali.IsAdministrator;
            item.UserImagePath = tumHali.UserImagePath;
            item.UserName = tumHali.UserName;
            item.UserCoverImagePath = tumHali.UserCoverImagePath; 
            #endregion
            bool sonuc = aus.Update(item);
            if (sonuc)
            {
                ViewBag.Message = "Bilgileriniz güncellendi.";
                return View(aus.GetByID(item.ID));
            }
            return View(aus.GetByID(item.ID));
        }
        public ActionResult ActivityInsert(Guid id)
        {
            return View(Tuple.Create<AppUser, Activity>(aus.GetByID(id), new Activity()));
        }
        [HttpPost]
        public ActionResult ActivityInsert([Bind(Prefix = "Item2")]Activity activity,bool delete)
        {
            AppUser gelen = (AppUser)Session["oturum"];
            if (delete)
            {
                return View(acs.Remove(activity.ID));
            }
            else
            {
                if (ModelState.IsValid)
                {
                    activity.AppUserID = gelen.ID;
                    acs.Add(activity);
                    ViewBag.Message = "Activity eklendi";
                }
                return RedirectToAction("ActivityPage", new { id = gelen.ID });
            }
        }
        public ActionResult ActivityPage(Guid id)
        {
            return View(Tuple.Create<List<Activity>,AppUser>(acs.GetActive(),aus.GetByID(id)));
        }
        public ActionResult ActivityUsers(Guid id)
        {
            AppUser gelen = (AppUser)Session["oturum"];
            return View(Tuple.Create<IEnumerable<ActivityGoingUser>, IEnumerable<ActivityThinkingUser>,AppUser>(agus.GetActive().Where(x => x.ActivityID == id), atus.GetActive().Where(x => x.ActivityID == id),aus.GetByID(gelen.ID)));
        }
        public ActionResult Notification(Guid id)
        {
            List<Like> likes = new List<Like>();
            List<Comment> comments = new List<Comment>();
            List<Follower> followers = new List<Follower>();
            foreach (Tweet tweet in ts.GetActive().Where(x => x.AppUserID == id))
            {
                if (ls.GetActive().Any(x=>x.TweetID==tweet.ID))
                {
                    foreach (Like like in ls.GetActive().Where(x=>x.TweetID==tweet.ID))
                    {
                        likes.Add(like);
                    }
                }
                if (cs.GetActive().Any(x=>x.TweetID==tweet.ID))
                {
                    foreach (Comment comment in cs.GetActive().Where(x=>x.TweetID==tweet.ID))
                    {
                        comments.Add(comment);
                    }
                }
            }
            if (fs.GetActive().Any(x=>x.DAppUserID==id))
            {
                foreach (Follower follower in fs.GetActive().Where(x=>x.DAppUserID==id))
                {
                    followers.Add(follower);
                }
            }
            return View(Tuple.Create<AppUser,List<Like>,List<Comment>,List<Follower>>(aus.GetByID(id),likes,comments,followers));
        }
        
    }
}
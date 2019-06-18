using Proje.Model.Entities;
using Proje.Service.Option;
using Proje.WebUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Proje.WebUI.Controllers
{
    public class LoginController : Controller
    {
        AppUserService aus = new AppUserService();
        CountryService counts = new CountryService();
        CityService cits = new CityService();
        UserLoginService lgs = new UserLoginService();
        Encryption enc = new Encryption();
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(AppUser item, UserLogin login)
        {
            var userName = "@" + item.UserName;
            List<AppUser> userToCheck = aus.GetDefault(x => x.UserName == userName);
            if (item.UserName != null || item.Password != null)
            {
                if (aus.Any(m => m.UserName == userName && item.Password != null))
                {
                    if (enc.ValidatePassword(item.Password, userToCheck[0].Password))
                    {
                        AppUser gelen = aus.FindByUsername(userName);
                        Session["oturum"] = gelen;
                        Session.Timeout = 20;
                        if (gelen.IsAdministrator == false)
                        {
                            ViewBag.Message = "Giriş başarılı";
                            login.AppUserID = gelen.ID;
                            lgs.Add(login);

                            return RedirectToAction("HomePage", "Home");
                        }
                        else
                        {
                            login.AppUserID = gelen.ID;
                            lgs.Add(login);
                            return RedirectToAction("Index", "AppUser", new { area = "Administrator" });
                        }
                    }
                    else
                    {
                        ViewBag.Message = "Kullanıcı Adı veya Şifre Hatalı.";
                    }
                }
                else
                {
                    ViewBag.Message = "Kullanıcı Adı veya Şifre Hatalı.";
                }
            }
            else
            {
                ViewBag.Message = "Kullanıcı Adı veya Şifre Boş Bırakılamaz.";
            }
            return View();
        }
        public ActionResult Register()
        {
            ViewBag.CountryID = new SelectList(counts.GetActive(), "ID", "CountryName");
            ViewBag.CityID = new SelectList(cits.GetActive(), "ID", "CityName");
            return View();
        }
        [HttpPost]
        public ActionResult Register(AppUser item, City city, Country country, HttpPostedFileBase[] userPhoto)
        {
            if (ModelState.IsValid)
            {
                item.UserName = "@" + item.UserName;
                var users = aus.GetAll();
                foreach (var user in users)
                {
                    if (user.UserName == item.UserName)
                    {
                        ViewBag.Message = "Bu Kullanıcı Adına Ait Kayıt Bulunmaktadır.";
                        ViewBag.CountryID = new SelectList(counts.GetActive(), "ID", "CountryName", country.ID);
                        ViewBag.CityID = new SelectList(cits.GetActive(), "CountryID", "CityName", city.ID);
                        return View();
                    }
                }
                if (country.ID != null && city.ID != null)
                {
                    item.Location.ID = Guid.NewGuid();
                }
                bool isUploaded;
                if (userPhoto != null)
                {
                    foreach (var itemPhoto in userPhoto)
                    {
                        if (itemPhoto != null)
                        {
                            if (itemPhoto.ContentType.Contains("image"))
                            {
                                string fileResult = FxFunction.Upload(userPhoto, FolderPath.User, out isUploaded);
                                if (isUploaded)
                                {
                                    item.UserImagePath = fileResult;
                                }
                            }
                        }
                        else
                        {
                            item.UserImagePath = "/Content/uploads/User/User.png";
                        }
                    }
                }
                string hashedPassword = enc.CreateHash(item.Password);
                item.Password = hashedPassword;
                bool sonuc = aus.Add(item);
                if (sonuc)
                {
                    if (item.IsAdministrator)
                    {
                        return RedirectToAction("Index");
                    }
                    return RedirectToAction("Login", "Login", new { area = "" });
                }
                else
                {
                    ViewBag.Message = "Tüm alanları doldurduğunuzdan emin olunuz.";
                }
            }
            else
            {
                ViewBag.Message = "Tüm alanları doldurduğunuzdan emin olunuz.";
            }
            ViewBag.CountryID = new SelectList(counts.GetActive(), "ID", "CountryName", country.ID);
            ViewBag.CityID = new SelectList(cits.GetActive(), "CountryID", "CityName", city.ID);

            return View();
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Login");
        }
    }
}
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Newtonsoft.Json;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Xml;
using ZBilet.Domain.Entities;
using ZBilet.UI.Models;

namespace ZBilet.UI.Controllers.User
{
    public class EventController : Controller
    {
        HttpClient _client;
        IHttpClientFactory _httpClientFactory;
        HttpResponseMessage _response;
        private readonly UserManager<AppUser> _userManager;
        public EventController(UserManager<AppUser> userManager = null, IHttpClientFactory httpClientFactory = null)
        {
            _client = new HttpClient();
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
        }

        private async Task<T> GetApiResponseAsync<T>(string apiUrl)
        {
            _response = await _client.GetAsync("https://api.zbilet.com/api/" + apiUrl);
            var jsonString = await _response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private async Task<T> PostApiResponseAsync<T>(string apiUrl, object payload)
        {
            var jsonObject = JsonConvert.SerializeObject(payload);
            var stringContent = new StringContent(jsonObject, Encoding.UTF8, "application/json");

            _response = await _client.PostAsync("https://api.zbilet.com/api/" + apiUrl, stringContent);
            var jsonString = await _response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private async Task<T> PutApiResponseAsync<T>(string apiUrl, object payload)
        {
            var jsonObject = JsonConvert.SerializeObject(payload);
            var stringContent = new StringContent(jsonObject, Encoding.UTF8, "application/json");

            _response = await _client.PutAsync("https://api.zbilet.com/api/" + apiUrl, stringContent);
            var jsonString = await _response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(jsonString);
        }
        private async Task<T> DeleteApiResponseAsync<T>(string apiUrl, int id)
        {
            _response = await _client.DeleteAsync("https://api.zbilet.com/api/" + apiUrl + "?id=" + id);
            var jsonString = await _response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
        public async Task<IActionResult> Index(int id, EventIndexViewModel model)
        {
            //Layout
            // Categories
            model.Categories = await GetApiResponseAsync<List<Category>>("Categories/Active");
            // Corporates
            model.Corporates = await GetApiResponseAsync<List<Corporate>>("Corporates/Active");
            // Subcategories
            model.Subcategories = await GetApiResponseAsync<List<Subcategory>>("Subcategories/Active");
            // Setting
            model.Setting = await GetApiResponseAsync<Setting>("Settings/1");

            //Controller
            //Places
            model.Places = await GetApiResponseAsync<List<Place>>("Places/Active");
            //Artists
            model.Artists = await GetApiResponseAsync<List<Artist>>("Artists/Active");
            //Prices
            model.Prices = await GetApiResponseAsync<List<Price>>("Prices/Events/" + id); ;
            //Event
            var item = await GetApiResponseAsync<Event>("Events/" + id);
            if (item != null)
            {
                item.Artist = model.Artists.FirstOrDefault(x => x.Id == item.ArtistId);
                item.Place = model.Places.FirstOrDefault(x => x.Id == item.PlaceId);
                model.Event = item;
                DateTime endDate = Convert.ToDateTime(model.Event.EventDate + model.Event.EventTime);
                var time = endDate.ToString("MMMM dd, yyyy HH:mm:ss");
                ViewBag.EventTime = endDate;
                //FavEvents
                var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (user != null)
                {
                    var favUserEvents = await GetApiResponseAsync<List<FavUserEvent>>("FavEvents/User/" + user.Id + "/" + model.Event.Id);
                    model.IsFavorited = (favUserEvents == null || user == null) ? false : favUserEvents.Count() > 0;
                }
                return View(model);
            }
            else
                return RedirectToAction("NotFound", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Index(int id)
        {
            var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var favUserEvents = await GetApiResponseAsync<List<FavUserEvent>>("FavEvents/User/" + user.Id + "/" + id);
            var isFavorited = (favUserEvents == null) ? false : favUserEvents.Count() > 0;
            if (!isFavorited)
            {
                FavUserEvent favUserEvent = new FavUserEvent
                {
                    UserId = user.Id,
                    EventId = id,
                };
                var favUserEventResponse = await PostApiResponseAsync<FavUserEvent>("FavEvents", favUserEvent);
                if (_response.IsSuccessStatusCode)
                    return RedirectToAction("Index", "Event", new { Id = id });
            }
            else
            {
                var fav = favUserEvents.FirstOrDefault(x => x.UserId == user.Id && x.EventId == id);
                var favUserArtistResponse = await DeleteApiResponseAsync<FavUserEvent>("FavEvents", fav.Id);
                if (_response.IsSuccessStatusCode)
                    return RedirectToAction("Index", "Event", new { Id = id });
            }
            return RedirectToAction("Index", "Event", new { Id = id });
        }
        public async Task<IActionResult> CheckOut(int id, EventIndexViewModel model, int? PriceNo)
        {
            //Layout
            // Categories
            model.Categories = await GetApiResponseAsync<List<Category>>("Categories/Active");
            // Corporates
            model.Corporates = await GetApiResponseAsync<List<Corporate>>("Corporates/Active");
            // Subcategories
            model.Subcategories = await GetApiResponseAsync<List<Subcategory>>("Subcategories/Active");
            // Setting
            model.Setting = await GetApiResponseAsync<Setting>("Settings/1");


            //Controller
            //Places
            model.Places = await GetApiResponseAsync<List<Place>>("Places/Active");
            //Artists
            model.Artists = await GetApiResponseAsync<List<Artist>>("Artists/Active");
            //Prices
            model.Prices = await GetApiResponseAsync<List<Price>>("Prices/Events/" + id); ;
            //Events
            var item = await GetApiResponseAsync<Event>("Events/" + id);
            if (item != null)
            {
                item.Prices = model.Prices.Where(x => x.Id == PriceNo).ToList();
                item.Artist = model.Artists.FirstOrDefault(x => x.Id == item.ArtistId);
                item.Place = model.Places.FirstOrDefault(x => x.Id == item.PlaceId);
                item.Category = model.Categories.FirstOrDefault(x => x.Id == item.CategoryId);
                item.Subcategory = model.Subcategories.FirstOrDefault(x => x.Id == item.SubcategoryId);
                model.Prices = model.Prices.Where(x => x.EventId == id).ToList();
                var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (user != null)
                {
                    model.Name = user.Name;
                    model.Surname = user.Surname;
                    model.EMail = user.Email;
                    model.PhoneNumber = user.PhoneNumber;
                }

                model.Event = item;
                ViewBag.SelectedPriceNo = PriceNo;
                return View(model);
            }
            else
                return RedirectToAction("NotFound", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CheckOut(int id, EventIndexViewModel model, int? PriceNo, string? cuponcode)
        {
            //Layout
            // Categories
            model.Categories = await GetApiResponseAsync<List<Category>>("Categories/Active");
            // Corporates
            model.Corporates = await GetApiResponseAsync<List<Corporate>>("Corporates/Active");
            // Subcategories
            model.Subcategories = await GetApiResponseAsync<List<Subcategory>>("Subcategories/Active");
            // Setting
            model.Setting = await GetApiResponseAsync<Setting>("Settings/1");

            //Controller
            //Places
            model.Places = await GetApiResponseAsync<List<Place>>("Places/Active");
            //Artists
            model.Artists = await GetApiResponseAsync<List<Artist>>("Artists/Active");
            //Prices
            model.Prices = await GetApiResponseAsync<List<Price>>("Prices/Events/" + id);
            //Event
            var item = await GetApiResponseAsync<Event>("Events/" + id);

            if (cuponcode != null)
            {
                //Discount
                var discount = await GetApiResponseAsync<Discount>("Discounts/Cupon/" + cuponcode);
                if (discount != null)
                {
                    model.DiscountRate = discount.DiscountRate;
                    model.DiscountCode = discount.DiscountCode;
                }

            }
            if (item != null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    model.User = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (model.User != null)
                    {
                        model.Name = model.User.Name;
                        model.Surname = model.User.Surname;
                        model.EMail = model.User.Email;
                        model.PhoneNumber = model.User.PhoneNumber;
                    }
                }
                item.Artist = model.Artists.FirstOrDefault(x => x.Id == item.ArtistId);
                item.Place = model.Places.FirstOrDefault(x => x.Id == item.PlaceId);
                item.Category = model.Categories.FirstOrDefault(x => x.Id == item.CategoryId);
                item.Subcategory = model.Subcategories.FirstOrDefault(x => x.Id == item.SubcategoryId);
                model.Event = item;
                model.Prices = model.Prices.Where(x => x.EventId == id).ToList();
                ViewBag.SelectedPriceNo = PriceNo;
                return View(model);
            }
            else
                return RedirectToAction("NotFound", "Home");
        }


        [HttpPost]
        public async Task<IActionResult> Payment(EventIndexViewModel model, int? id, int? category, int? quantity)
        {
            var Price = await GetApiResponseAsync<Price>("Prices/" + category);
            if (category != null && quantity != null && Price.EventId == id && Price.isFull == false)
            {
                decimal? CalculateAmount;
                decimal? CategoryPrice = Price.Cost;
                CalculateAmount = quantity * CategoryPrice;
                if (model.DiscountCode != null)
                {
                    var discount = await GetApiResponseAsync<Discount>("Discounts/Cupon/" + model.DiscountCode);
                    if (discount != null)
                    {
                        model.DiscountRate = discount.DiscountRate;
                        CalculateAmount = (CalculateAmount - (CalculateAmount * model.DiscountRate / 100));
                        model.Amount = CalculateAmount?.ToString("0.00");
                        model.Amount = model.Amount.Replace(",", ".");
                    }
                }
                else
                {
                    model.DiscountRate = null;
                    model.Amount = CalculateAmount?.ToString("0.00");
                    model.Amount = model.Amount.Replace(",", ".");
                }
            }
            else
            {
                return RedirectToAction("NotFound", "Home");
            }

            model.ExpirationMonth = int.Parse(model.ExpirationMonth) < 10 ? "0" + model.ExpirationMonth.ToString() : model.ExpirationMonth.ToString();
            string ExpirationDate = model.ExpirationYear.Substring(model.ExpirationYear.Length - 2) + model.ExpirationMonth;
            int BrandNumber = 100;
            if (model.CardNumber.Substring(0, 1) == "4")
            {
                BrandNumber = 100;
            }
            if (model.CardNumber.Substring(0, 1) == "5")
            {
                BrandNumber = 200;
            }
            if (model.CardNumber.Substring(0, 1) == "6")
            {
                BrandNumber = 300;
            }

            string data = "Pan=" + model.CardNumber + "&ExpiryDate=" + ExpirationDate + "&PurchaseAmount=" +
                model.Amount + "&Currency=949&BrandName=" + BrandNumber + "&VerifyEnrollmentRequestId=" + Guid.NewGuid().ToString("N") +
                "&SessionInfo=Cvv:" + model.Cvv + ",Date:" + model.ExpirationYear + model.ExpirationMonth + ",PriceId:" + category +
                ",Quantity:" + quantity + ",EventId:" + id + ",DiscountCode:" + model.DiscountCode +
                "&MerchantID=000000011182250&MerchantPassword=Qx96KrBt&FailureUrl=https://zbilet.com/Event/FailureUrl" +
                "&SuccessUrl=https://zbilet.com/Event/SuccessUrl"; //replace <value>
            byte[] dataStream = Encoding.UTF8.GetBytes(data);
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("https://3dsecure.vakifbank.com.tr/MPIAPI/MPI_Enrollment.aspx"); //Mpi Enrollment Adresi
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = dataStream.Length;
            webRequest.KeepAlive = false;
            string responseFromServer = "";

            using (Stream newStream = webRequest.GetRequestStream())
            {
                newStream.Write(dataStream, 0, dataStream.Length);
                newStream.Close();
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseFromServer = reader.ReadToEnd();
                    reader.Close();
                }

                webResponse.Close();
            }

            if (string.IsNullOrEmpty(responseFromServer))
            {
                int a = 0;

            }

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseFromServer);


            var statusNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/Status");
            var pareqNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/PaReq");
            var acsUrlNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/ACSUrl");
            var termUrlNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/TermUrl");
            var mdNode = xmlDocument.SelectSingleNode("IPaySecure/Message/VERes/MD");
            var messageErrorCodeNode = xmlDocument.SelectSingleNode("IPaySecure/MessageErrorCode");

            string statusText = "";

            if (statusNode != null)
            {
                statusText = statusNode.InnerText;
            }

            //3d secure programına dahil
            if (statusText == "Y")
            {
                string postBackForm =
                   @"<html>
                          <head>
                            <meta name=""viewport"" content=""width=device-width"" />
                            <title>MpiForm</title>
                            <script>
                              function postPage() {
                              document.forms[""frmMpiForm""].submit();
                              }
                            </script>
                          </head>
                          <body onload=""javascript:postPage();"">
                            <form action=""@ACSUrl"" method=""post"" id=""frmMpiForm"" name=""frmMpiForm"">
                              <input type=""hidden"" name=""PaReq"" value=""@PAReq"" />
                              <input type=""hidden"" name=""TermUrl"" value=""@TermUrl"" />
                              <input type=""hidden"" name=""MD"" value=""@MD "" />
                              <noscript>
                                <input type=""submit"" id=""btnSubmit"" value=""Gönder"" />
                              </noscript>
                            </form>
                          </body>
                        </html>";

                postBackForm = postBackForm.Replace("@ACSUrl", acsUrlNode.InnerText);
                postBackForm = postBackForm.Replace("@PAReq", pareqNode.InnerText);
                postBackForm = postBackForm.Replace("@TermUrl", termUrlNode.InnerText);
                postBackForm = postBackForm.Replace("@MD", mdNode.InnerText);

                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = postBackForm
                };
            }
            else if (statusText == "E")
            {
                string errorCode = messageErrorCodeNode.InnerText;
                return RedirectToAction("Failure", "Event");
            }
            return RedirectToAction("Failure", "Event");
        }

        public async Task<IActionResult> SuccessUrl(PaymentViewModel model)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SuccessUrl(PaymentViewModel model, int? id)
        {
            // Layout
            // Categories
            model.Categories = await GetApiResponseAsync<List<Category>>("Categories/Active");
            // Corporates
            model.Corporates = await GetApiResponseAsync<List<Corporate>>("Corporates/Active");
            // Subcategories
            model.Subcategories = await GetApiResponseAsync<List<Subcategory>>("Subcategories/Active");
            // Setting
            model.Setting = await GetApiResponseAsync<Setting>("Settings/1");

            model.Status = Request.Form["Status"];
            model.MerchantId = Request.Form["MerchantId"];
            model.VerifyEnrollmentRequestId = Request.Form["VerifyEnrollmentRequestId"];
            model.Xid = Request.Form["Xid"];
            model.PurchAmount = Request.Form["PurchAmount"];
            model.SessionInfo = Request.Form["SessionInfo"];
            model.PurchCurrency = Request.Form["PurchCurrency"];
            model.Pan = Request.Form["Pan"];
            model.Cvv = Request.Form["Cvv"];
            model.ExpiryDate = Request.Form["Expiry"];
            model.Eci = Request.Form["Eci"];
            model.Cavv = Request.Form["Cavv"];
            model.InstallmentCount = Request.Form["InstallmentCount"];

            if (model.PurchAmount.Length > 2)
            {
                model.PurchAmount = model.PurchAmount.Substring(0, model.PurchAmount.Length - 2) + "." + model.PurchAmount.Substring(model.PurchAmount.Length - 2);
            }

            string[] InfoParts = model.SessionInfo.Split(',');

            string cvv = InfoParts[0].Split(':')[1];
            string date = InfoParts[1].Split(':')[1];
            string priceId = InfoParts[2].Split(':')[1];
            int quantity = int.Parse(InfoParts[3].Split(':')[1]);
            string eventId = InfoParts[4].Split(':')[1];
            string discountCode = InfoParts[5].Split(':')[1];


            XmlDocument xmlDoc = new XmlDocument();

            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            XmlElement rootNode = xmlDoc.CreateElement("VposRequest");
            xmlDoc.InsertBefore(xmlDeclaration, xmlDoc.DocumentElement);
            xmlDoc.AppendChild(rootNode);

            //eklemek istedi�iniz di�er elementleride bu �ekilde ekleyebilirsiniz.
            XmlElement merchantNode = xmlDoc.CreateElement("MerchantId");
            XmlElement passwordNode = xmlDoc.CreateElement("Password");
            XmlElement terminalNode = xmlDoc.CreateElement("TerminalNo");
            XmlElement transactionTypeNode = xmlDoc.CreateElement("TransactionType");
            XmlElement transactionIdNode = xmlDoc.CreateElement("TransactionId");
            XmlElement currencyAmountNode = xmlDoc.CreateElement("CurrencyAmount");
            XmlElement currencyCodeNode = xmlDoc.CreateElement("CurrencyCode");
            XmlElement panNode = xmlDoc.CreateElement("Pan");
            XmlElement cvvNode = xmlDoc.CreateElement("Cvv");
            XmlElement expiryNode = xmlDoc.CreateElement("Expiry");
            XmlElement eciNode = xmlDoc.CreateElement("ECI");
            XmlElement cavvNode = xmlDoc.CreateElement("CAVV");
            XmlElement mpiTransationIdNode = xmlDoc.CreateElement("MpiTransactionId");
            XmlElement ClientIpNode = xmlDoc.CreateElement("ClientIp");
            XmlElement transactionDeviceSourceNode = xmlDoc.CreateElement("TransactionDeviceSource");

            //yukar�da ekledi�imiz node lar i�in de�erleri ekliyoruz.
            XmlText merchantText = xmlDoc.CreateTextNode(model.MerchantId);
            XmlText passwordtext = xmlDoc.CreateTextNode("Qx96KrBt");
            XmlText terminalNoText = xmlDoc.CreateTextNode("VB295276");
            XmlText transactionTypeText = xmlDoc.CreateTextNode("Sale");
            XmlText transactionIdText = xmlDoc.CreateTextNode(Guid.NewGuid().ToString("N")); //uniqe olacak �ekilde d�zenleyebilirsiniz.
            XmlText currencyAmountText = xmlDoc.CreateTextNode(model.PurchAmount); //tutar� nokta ile g�nderdi�inizden emin olunuz.
            XmlText currencyCodeText = xmlDoc.CreateTextNode(model.PurchCurrency);
            XmlText panText = xmlDoc.CreateTextNode(model.Pan);
            XmlText cvvText = xmlDoc.CreateTextNode(cvv);
            XmlText expiryText = xmlDoc.CreateTextNode(date);
            XmlText eciText = xmlDoc.CreateTextNode(model.Eci);
            XmlText cavvText = xmlDoc.CreateTextNode(model.Cavv);
            XmlText mpiTransationIdText = xmlDoc.CreateTextNode(model.VerifyEnrollmentRequestId);
            XmlText ClientIpText = xmlDoc.CreateTextNode("45.151.250.153");
            XmlText transactionDeviceSourceText = xmlDoc.CreateTextNode("0");

            //nodelar� root elementin alt�na ekliyoruz.
            rootNode.AppendChild(merchantNode);
            rootNode.AppendChild(passwordNode);
            rootNode.AppendChild(terminalNode);
            rootNode.AppendChild(transactionTypeNode);
            rootNode.AppendChild(transactionIdNode);
            rootNode.AppendChild(currencyAmountNode);
            rootNode.AppendChild(currencyCodeNode);
            rootNode.AppendChild(panNode);
            rootNode.AppendChild(cvvNode);
            rootNode.AppendChild(expiryNode);
            rootNode.AppendChild(eciNode);
            rootNode.AppendChild(cavvNode);
            rootNode.AppendChild(mpiTransationIdNode);
            rootNode.AppendChild(ClientIpNode);
            rootNode.AppendChild(transactionDeviceSourceNode);

            //nodelar i�in olu�turdu�umuz textleri node lara ekliyoruz.
            merchantNode.AppendChild(merchantText);
            passwordNode.AppendChild(passwordtext);
            terminalNode.AppendChild(terminalNoText);
            transactionTypeNode.AppendChild(transactionTypeText);
            transactionIdNode.AppendChild(transactionIdText);
            currencyAmountNode.AppendChild(currencyAmountText);
            currencyCodeNode.AppendChild(currencyCodeText);
            panNode.AppendChild(panText);
            cvvNode.AppendChild(cvvText);
            expiryNode.AppendChild(expiryText);
            eciNode.AppendChild(eciText);
            cavvNode.AppendChild(cavvText);
            mpiTransationIdNode.AppendChild(mpiTransationIdText);
            ClientIpNode.AppendChild(ClientIpText);
            transactionDeviceSourceNode.AppendChild(transactionDeviceSourceText);

            string xmlMessage = xmlDoc.OuterXml;

            //olu�turdu�umuz xml i vposa g�nderiyoruz.
            byte[] dataStream = Encoding.UTF8.GetBytes("prmstr=" + xmlMessage);
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create("https://onlineodeme.vakifbank.com.tr:4443/VposService/v3/Vposreq.aspx");//Vpos adresi
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = dataStream.Length;
            webRequest.KeepAlive = false;
            string responseFromServer = "";

            using (Stream newStream = webRequest.GetRequestStream())
            {
                newStream.Write(dataStream, 0, dataStream.Length);
                newStream.Close();
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseFromServer = reader.ReadToEnd();
                    reader.Close();
                }

                webResponse.Close();
            }

            if (string.IsNullOrEmpty(responseFromServer))
            {
                ViewBag.Response = "Vpos cevap vermedi";
                return RedirectToAction("Failure", "Event");
            }
            var xmlResponse = new XmlDocument();
            xmlResponse.LoadXml(responseFromServer);
            var resultCodeNode = xmlResponse.SelectSingleNode("VposResponse/ResultCode");
            var resultDescriptionNode = xmlResponse.SelectSingleNode("VposResponse/ResultDescription");
            string resultCode = "";
            string resultDescription = "";

            if (resultCodeNode != null)
            {
                resultCode = resultCodeNode.InnerText;
            }
            if (resultDescriptionNode != null)
            {
                resultDescription = resultDescriptionNode.InnerText;
            }

            if (resultCode == "0000")
            {
                if (quantity > 1)
                {
                    if (discountCode != null)
                    {
                        var discount = await GetApiResponseAsync<Discount>("Discounts/" + discountCode);
                        discount.DiscountUse += 1;
                        var response = await PutApiResponseAsync<Discount>("Discounts", discount);
                    }

                    model.Price = await GetApiResponseAsync<Price>("Prices/" + priceId);
                    model.Event = await GetApiResponseAsync<Event>("Events/" + eventId);

                    model.Event.EventCapacity = model.Event.EventCapacity - quantity;
                    var response2 = await PutApiResponseAsync<Event>("Events", model.Event);
                    if (!User.Identity.IsAuthenticated)
                    {
                        model.Ticket.UserId = 10012;
                    }
                    else
                    {
                        var user = await _userManager.GetUserAsync(User);
                        model.Ticket.UserId = user.Id;
                    }
                    model.Ticket.EventId = model.Event.Id;
                    model.Ticket.ArtistId = model.Event.ArtistId;
                    model.Ticket.Price = Decimal.Parse(model.PurchAmount);
                    model.Ticket.PriceId = model.Price.Id;
                    model.Ticket.PurchasedDate = DateTime.Now;
                    model.Ticket.IsActive = true;
                    model.Ticket.IsCompleted = false;
                    model.Ticket.IsAddedToGroup = false;
                    for (int j = 1; j <= quantity; j++)
                    {
                        Random random = new Random();
                        string alphabet = "abcdefghijklmnopqrstuvwxyz";
                        string word = "";
                        for (int i = 0; i < 8; i++)
                        {
                            word += alphabet[random.Next(alphabet.Length)];
                        }
                        model.Ticket.TicketCode = word;
                        var response3 = await PostApiResponseAsync<PaymentViewModel>("Tickets", model.Ticket);
                    }
                }
                else if (quantity == 1)
                {
                    if (discountCode != null)
                    {
                        var discount = await GetApiResponseAsync<Discount>("Discounts/" + discountCode);
                        discount.DiscountUse += 1;
                        var response = await PutApiResponseAsync<Discount>("Discounts", discount);
                    }
                    model.Price = await GetApiResponseAsync<Price>("Prices/" + priceId);
                    model.Event = await GetApiResponseAsync<Event>("Events/" + eventId);

                    model.Event.EventCapacity = model.Event.EventCapacity - 1;
                    var response2 = await PutApiResponseAsync<Event>("Events", model.Event);

                    Random random = new Random();
                    string alphabet = "abcdefghijklmnopqrstuvwxyz";
                    string word = "";
                    for (int i = 0; i < 8; i++)
                    {
                        word += alphabet[random.Next(alphabet.Length)];
                    }
                    model.Ticket.UserId = 10012;
                    model.Ticket.EventId = model.Event.Id;
                    model.Ticket.ArtistId = model.Event.ArtistId;
                    model.Ticket.Price = Decimal.Parse(model.PurchAmount);
                    model.Ticket.PriceId = model.Price.Id;
                    model.Ticket.PurchasedDate = DateTime.Now;
                    model.Ticket.IsActive = true;
                    model.Ticket.IsCompleted = false;
                    model.Ticket.IsAddedToGroup = false;
                    model.Ticket.TicketCode = word;
                    var response3 = await PostApiResponseAsync<PaymentViewModel>("Tickets", model.Ticket);
                    if (_response.IsSuccessStatusCode)
                    {
                        ViewBag.Response = "Islem Sonucu " + resultCode + " " + resultDescription;
                        return View(model);
                    }
                }
                ViewBag.Response = "Islem Sonucu " + resultCode + " " + resultDescription;
                return View(model);

            }

            else
            {
                if (quantity > 1)
                {
                    if (discountCode != null || discountCode.Length > 0)
                    {
                        var discount = await GetApiResponseAsync<Discount>("Discounts/Cupon/" + discountCode);
                        discount.DiscountUse += 1;
                        var response = await PutApiResponseAsync<Discount>("Discounts", discount);
                    }

                    model.Price = await GetApiResponseAsync<Price>("Prices/" + priceId);
                    model.Event = await GetApiResponseAsync<Event>("Events/" + eventId);

                    model.Event.EventCapacity = model.Event.EventCapacity - quantity;
                    var response2 = await PutApiResponseAsync<Event>("Events", model.Event);
                    if (!User.Identity.IsAuthenticated)
                    {
                        model.Ticket.UserId = 10012;
                    }
                    else
                    {
                        var user = await _userManager.GetUserAsync(User);
                        model.Ticket.UserId = user.Id;
                    }
                    model.Ticket.EventId = model.Event.Id;
                    model.Ticket.ArtistId = model.Event.ArtistId;
                    model.Ticket.Price = Decimal.Parse(model.PurchAmount);
                    model.Ticket.PriceId = model.Price.Id;
                    model.Ticket.PurchasedDate = DateTime.Now;
                    model.Ticket.IsActive = true;
                    model.Ticket.IsCompleted = false;
                    model.Ticket.IsAddedToGroup = false;
                    for (int j = 1; j <= quantity; j++)
                    {
                        Random random = new Random();
                        string alphabet = "abcdefghijklmnopqrstuvwxyz";
                        string word = "";
                        for (int i = 0; i < 8; i++)
                        {
                            word += alphabet[random.Next(alphabet.Length)];
                        }
                        model.Ticket.TicketCode = word;
                        var response3 = await PostApiResponseAsync<PaymentViewModel>("Tickets", model.Ticket);
                    }
                }
                else if (quantity == 1)
                {
                    //if (discountCode != null || discountCode.Length > 0)
                    //{
                    //    var discount = await GetApiResponseAsync<Discount>("Discounts/Cupon/" + discountCode);
                    //    discount.DiscountUse += 1;
                    //    var response = await PutApiResponseAsync<Discount>("Discounts", discount);
                    //}
                    model.Price = await GetApiResponseAsync<Price>("Prices/" + priceId);
                    model.Event = await GetApiResponseAsync<Event>("Events/" + eventId);

                    model.Event.EventCapacity = model.Event.EventCapacity - 1;
                    var response2 = await PutApiResponseAsync<Event>("Events", model.Event);

                    Random random = new Random();
                    string alphabet = "abcdefghijklmnopqrstuvwxyz";
                    string word = "";
                    for (int i = 0; i < 8; i++)
                    {
                        word += alphabet[random.Next(alphabet.Length)];
                    }
                    //model.Ticket.UserId = 10012;
                    //model.Ticket.EventId = model.Event.Id;
                    //model.Ticket.ArtistId = model.Event.ArtistId;
                    //model.Ticket.Price = model.Price.Cost;
                    //model.Ticket.PriceId = model.Price.Id;
                    //model.Ticket.PurchasedDate = DateTime.Now;
                    //model.Ticket.IsActive = true;
                    //model.Ticket.IsCompleted = false;
                    //model.Ticket.IsAddedToGroup = false;
                    //model.Ticket.TicketCode = word;
                    //var response3 = await PostApiResponseAsync<PaymentViewModel>("Tickets", model.Ticket);
                    //if (_response.IsSuccessStatusCode)
                    //{
                    //    ViewBag.Response = "Islem Sonucu " + resultCode + " " + resultDescription;
                    //    return View(model);
                    //}
                    return RedirectToAction("SuccessUrl", "Event");
                }
                ViewBag.Response = "Islem Sonucu " + resultCode + " " + resultDescription;
                return View(model);
                //ViewBag.Response = "Islem Sonucu " + resultCode + " " + resultDescription;
                //return RedirectToAction("Failure", "Event");
            }


        }

        public async Task<IActionResult> Failure(PaymentViewModel model)
        {
            // Layout
            // Categories
            model.Categories = await GetApiResponseAsync<List<Category>>("Categories/Active");
            // Corporates
            model.Corporates = await GetApiResponseAsync<List<Corporate>>("Corporates/Active");
            // Subcategories
            model.Subcategories = await GetApiResponseAsync<List<Subcategory>>("Subcategories/Active");
            // Setting
            model.Setting = await GetApiResponseAsync<Setting>("Settings/1");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Failure(PaymentViewModel model, int id)
        {
            // Layout
            // Categories
            model.Categories = await GetApiResponseAsync<List<Category>>("Categories/Active");
            // Corporates
            model.Corporates = await GetApiResponseAsync<List<Corporate>>("Corporates/Active");
            // Subcategories
            model.Subcategories = await GetApiResponseAsync<List<Subcategory>>("Subcategories/Active");
            // Setting
            model.Setting = await GetApiResponseAsync<Setting>("Settings/1");
            return View(model);
        }
    }
}

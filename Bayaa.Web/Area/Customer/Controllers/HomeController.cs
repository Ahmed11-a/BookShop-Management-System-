using Bayaa.DataAccess.Repository.IRepository;
using Bayaa.Models.Models;
using Bayaa.Utilty;
using Bayaa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace Bayaa.Areas.Customer.Controllers
{
    [Area(nameof(Customer))]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private readonly IUnitOfWork _unitOfWork;

		public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork)
        {
            _logger = logger;
			_unitOfWork = unitOfWork;
		}

        public IActionResult Index()
        { 
            IEnumerable<Product> productList=_unitOfWork.Product.GetAndInclude(agers: "Category");
            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new()
            {
                Product = _unitOfWork.Product.GetInclud(p => p.Id == productId, "Category"),
                Count = 1,
                ProductId = productId
            };
            
            return View(cart);
        }
        [HttpPost]
        [Authorize]
		public IActionResult Details(ShoppingCart shoppingCart)
		{
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId= UserId;

			ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == UserId &&
			u.ProductId == shoppingCart.ProductId);

            if (cartFromDb != null)
            {
                //shopping cart exists
                cartFromDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            else
            {
                //add cart record
                _unitOfWork.ShoppingCart.Add(shoppingCart);
				_unitOfWork.Save();
				HttpContext.Session.SetInt32(SD.SessionCart,
              _unitOfWork.ShoppingCart.GetAndInclude(u => u.ApplicationUserId == UserId).Count());
            }
			TempData["success"] = "Cart Added successfully";


			return RedirectToAction(nameof(Index));
		}

		public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

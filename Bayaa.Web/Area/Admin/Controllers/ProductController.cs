
using Bayaa.DataAccess.Repository.IRepository;
using Bayaa.Models.Models;
using Bayaa.Models.ViewModel;
using Bayaa.Utilty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bayaa.Areas.Admin.Controllers
{
	[Area(nameof(Admin))]
	[Authorize(Roles = (SD.Role_Admin))]
	public class ProductController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
		}

		public IActionResult Index()
		{

			IEnumerable<Product> productList = _unitOfWork.Product.GetAndInclude(agers:"Category");
			return View(productList);

		}

		[HttpGet]
		public IActionResult Upsert(int? id)
		{
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };
            if (id == null || id == 0)
			{
				// create 
				return View(productVM);
			}
			else
			{
				//update 
				productVM.Product = _unitOfWork.Product.Get(p => p.Id == id);
				return View(productVM);
			}


			return View(productVM);
		}

		[HttpPost]
		public IActionResult Upsert(ProductVM productVM, IFormFile? file)
		{

			var product = _unitOfWork.Product.GetById(productVM.Product.Id);
			if (ModelState.IsValid)
			{
				string wwwRootPath = _webHostEnvironment.WebRootPath;
				if (file != null)
				{
					string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string productPath = Path.Combine(wwwRootPath, @"images\products\");

					if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
					{
						var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
						if (!Directory.Exists(oldImagePath))
						{
							Directory.CreateDirectory(oldImagePath);

						}

					}


					using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
					{
						file.CopyTo(fileStream);
					}
					productVM.Product.ImageUrl = @"\images\products\" + fileName;

				}
				var mm = string.Empty;
				if (productVM.Product.Id == 0)
				{
					_unitOfWork.Product.Add(productVM.Product);
					mm = "Product Created  Success";
				}
				else
				{
					product.CategoryId = productVM.Product.CategoryId;
					_unitOfWork.Product.Update(productVM.Product);
					mm = "Product Updated  Success";
				}

				_unitOfWork.Save();
				TempData["Success"] =mm;
				return RedirectToAction(nameof(Index));

			}
			else
			{
				productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
				{
					Text = u.Name,
					Value = u.Id.ToString()
				});

			}
			return View(productVM);

		}

		//[HttpPost]
		//public IActionResult Upsert(ProductVM productVM, IFormFile file)
		//{
		//    if (ModelState.IsValid)
		//    {
		//        if (productVM.Product.Id == 0)
		//        {
		//            _unitOfWork.Product.Add(productVM.Product);
		//        }
		//        else
		//        {
		//            _unitOfWork.Product.Update(productVM.Product);
		//        }
		//        _unitOfWork.Save();
		//        string wwwRootPath = _webHostEnvironment.WebRootPath;
		//        if (file != null)
		//        {
		//                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
		//                string productPath = @"images\products" + productVM.Product.Id;
		//                string finalPath = Path.Combine(wwwRootPath, productPath);

		//                if (!Directory.Exists(finalPath))
		//                    Directory.CreateDirectory(finalPath);

		//                using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
		//                {
		//                    file.CopyTo(fileStream);
		//                }
		//        }
		//            _unitOfWork.Product.Update(productVM.Product);
		//            _unitOfWork.Save();
		//        TempData["success"] = "Product created/updated successfully";
		//        return RedirectToAction("Index");
		//    }
		//    else
		//    {
		//        productVM.CategoryList = _unitOfWork.Category.GetAll();
		//        return View(productVM);
		//    }
		//}


		[HttpDelete]
		public IActionResult Delete(int? id)
		{
			Product product = _unitOfWork.Product.Get(c => c.Id == id);
			if (product == null)
			{
				return Json(new {success=false,message="Error while deleted"});
			}
			var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,product.ImageUrl.TrimStart('\\'));
			if (System.IO.File.Exists(oldImagePath))
			{
				System.IO.File.Delete(oldImagePath);

			}
			_unitOfWork.Product.Remove(product);
			_unitOfWork.Save();
			TempData["success"] = "Product Deleted Successfully";
			return Json(new { success = true, message = "Product deleted Successfully" });

		}
		#region Json
		public IActionResult GetAll()
		{


			IEnumerable<Product> productList = _unitOfWork.Product.GetAndInclude(agers:"Category");
			return Json(new { Data = productList });

		}
		#endregion

	}
}

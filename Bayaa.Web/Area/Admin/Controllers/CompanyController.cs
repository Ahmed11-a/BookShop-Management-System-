
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
	public class CompanyController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		public CompanyController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{

			IEnumerable<Company> CompanyList = _unitOfWork.Company.GetAll();
			return View(CompanyList);

		}

		[HttpGet]
		public IActionResult Upsert(int? id)
		{
			
			if (id == null || id == 0)
			{
				// create 
				return View(new Company());
			}
			else
			{
				//update 
				Company company = _unitOfWork.Company.Get(p => p.Id == id);
				return View(company);
			}
		}

		[HttpPost]
		public IActionResult Upsert(Company company)
		{


			if (ModelState.IsValid)
			{
				var message = string.Empty;
				if (company.Id == 0)
				{
					_unitOfWork.Company.Add(company);
                    message = "Company Created  Success";
				}
				else
				{
					_unitOfWork.Company.Update(company);
                    message = "Company Updated  Success";
				}

				_unitOfWork.Save();
				TempData["Success"] = message;
				return RedirectToAction(nameof(Index));

			}
			else
			{
                return View(company);

            }

        }


		[HttpDelete]
		public IActionResult Delete(int? id)
		{
			Company Company = _unitOfWork.Company.Get(c => c.Id == id);
			if (Company == null)
			{
				return Json(new {success=false,message="Error while deleted"});
			}
			_unitOfWork.Company.Remove(Company);
			_unitOfWork.Save();
			TempData["success"] = "Company Deleted Successfully";
			return Json(new { success = true, message = "Company deleted Successfully" });

		}
		#region Json
		public IActionResult GetAll()
		{


			IEnumerable<Company> CompanyList = _unitOfWork.Company.GetAll();
			return Json(new { Data = CompanyList });

		}
		#endregion

	}
}


using Bayaa.DataAccess.Data;
using Bayaa.DataAccess.Repository.IRepository;
using Bayaa.Models.Models;
using Bayaa.Models.ViewModel;
using Bayaa.Utilty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bayaa.Areas.Admin.Controllers
{
	[Area(nameof(Admin))]
	[Authorize(Roles = (SD.Role_Admin))]
	public class UserController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ApplicationDbContext _context;
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;

		public UserController(IUnitOfWork unitOfWork,
			ApplicationDbContext context,
			UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager)
		{
			_unitOfWork = unitOfWork;
			_userManager = userManager;
			_context = context;
			_roleManager = roleManager;
		}

		public IActionResult Index()
		{

			return View();

		}


		[HttpDelete]
		public IActionResult Delete(int? id)
		{
			
			return Json(new { success = true, message = "Company deleted Successfully" });

		}

		public IActionResult RoleManagment(string userId)
		{

			RoleManagementVM RoleVM = new RoleManagementVM()
			{
				ApplicationUser = _unitOfWork.ApplicationUser.GetInclud(u => u.Id == userId, ager: "Company"),
				RoleList = _roleManager.Roles.Select(i => new SelectListItem
				{
					Text = i.Name,
					Value = i.Name
				}),
				CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
				{
					Text = i.Name,
					Value = i.Id.ToString()
				}),
			};

			RoleVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId))
					.GetAwaiter().GetResult().FirstOrDefault();
			return View(RoleVM);
		}

		[HttpPost]
		public IActionResult RoleManagment(RoleManagementVM roleManagmentVM)
		{

			string oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == roleManagmentVM.ApplicationUser.Id))
					.GetAwaiter().GetResult().FirstOrDefault();

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagmentVM.ApplicationUser.Id);


			if (!(roleManagmentVM.ApplicationUser.Role == oldRole))
			{
				//a role was updated
				if (roleManagmentVM.ApplicationUser.Role == SD.Role_Company)
				{
					applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
				}
				if (oldRole == SD.Role_Company)
				{
					applicationUser.CompanyId = null;
				}
				_unitOfWork.ApplicationUser.Update(applicationUser);
				_unitOfWork.Save();

				_userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
				_userManager.AddToRoleAsync(applicationUser, roleManagmentVM.ApplicationUser.Role).GetAwaiter().GetResult();

			}
			else
			{
				if (oldRole == SD.Role_Company && applicationUser.CompanyId != roleManagmentVM.ApplicationUser.CompanyId)
				{
					applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
					_unitOfWork.ApplicationUser.Update(applicationUser);
					_unitOfWork.Save();
				}
			}

			return RedirectToAction("Index");
		}

            [HttpPost]
		public IActionResult LockUnlock([FromBody] string id)
		{

			var objFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
			if (objFromDb == null)
			{
				return Json(new { success = false, message = "Error while Locking/Unlocking" });
			}

			if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
			{
				//user is currently locked and we need to unlock them
				objFromDb.LockoutEnd = DateTime.Now;
			}
			else
			{
				objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
			}
			//_unitOfWork.ApplicationUser.(objFromDb);
			_unitOfWork.Save();
			return Json(new { success = true, message = "Operation Successful" });
		}

		#region Json
		public IActionResult GetAll()
		{


			IEnumerable<ApplicationUser> usertList = _unitOfWork.ApplicationUser.GetAndInclude(agers:"Company");

			foreach (var user in usertList)
			{

				user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

				if (user.Company == null)
				{
					user.Company = new Company()
					{
						Name = ""
					};
				}
			}

			return Json(new { data = usertList });
		}

	}
		#endregion

	
}

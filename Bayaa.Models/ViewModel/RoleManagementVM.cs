using Bayaa.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bayaa.Models.ViewModel
{
	public class RoleManagementVM
	{
		public ApplicationUser ApplicationUser { get; set; }
		public IEnumerable<SelectListItem> RoleList { get; set; }
		public IEnumerable<SelectListItem> CompanyList { get; set; }

	}
}

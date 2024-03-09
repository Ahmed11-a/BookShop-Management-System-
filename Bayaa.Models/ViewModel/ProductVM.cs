using Bayaa.Models.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bayaa.Models.ViewModel
{
	public class ProductVM
	{
	  public Product Product { get; set; }
		[ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; }
	}
}

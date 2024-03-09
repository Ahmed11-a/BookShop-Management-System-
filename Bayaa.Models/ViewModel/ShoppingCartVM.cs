using Bayaa.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bayaa.Models.ViewModel
{
	public class ShoppingCartVM
	{
		public IEnumerable<ShoppingCart> shoppingCartList { get; set; }
		public OrderHeader OrderHeader { get; set; }
	}
}

using Bayaa.DataAccess.Repository.IRepository;
using Bayaa.Models.Models;
using Bayaa.Models.ViewModel;
using Bayaa.Utilty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace Bayaa.Web.Areas.Admin.Controllers
{
    [Area(nameof(Admin))]
	[Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		[BindProperty]
        public OrderVM orderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

		public IActionResult Details(int orderId)
		{
		  orderVM = new()
			{
				OrderHeader = _unitOfWork.OrderHeader.GetInclud(o => o.Id == orderId, "ApplicationUser"),
				OrderDetail=_unitOfWork.OrderDetail.GetAndInclude(o => o.OrderHeaderId == orderId, "Product")

			};

			return View(orderVM);
		}

        [HttpPost]
        [Authorize(Roles=SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {

            var orderHeader = _unitOfWork.OrderHeader.GetById(orderVM.OrderHeader.Id);
            orderHeader.Name = orderVM.OrderHeader.Name;
            orderHeader.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeader.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderHeader.City = orderVM.OrderHeader.City;
            orderHeader.State = orderVM.OrderHeader.State;
            orderHeader.PostalCode = orderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeader.Carrier = orderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderHeader.Carrier = orderVM.OrderHeader.TrackingNumber;
            }
            TempData["Success"] = "Order Details Updated Successfully.";

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();



            return RedirectToAction(nameof(Details), new { orderId=orderHeader.Id });
        }


		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult StartProcessing()
		{
			_unitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
			_unitOfWork.Save();
			TempData["Success"] = "Order Details Updated Successfully.";
			return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
		}

		[HttpPost]
		[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
		public IActionResult ShipOrder()
		{

			var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);
			orderHeader.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
			orderHeader.Carrier = orderVM.OrderHeader.Carrier;
			orderHeader.OrderStatus = SD.StatusShipped;
			orderHeader.ShippingDate = DateTime.Now;
			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
			}

			_unitOfWork.OrderHeader.Update(orderHeader);
			_unitOfWork.Save();
			TempData["Success"] = "Order Shipped Successfully.";
			return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
		}

		public IActionResult CancelOrder()
		{

			var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderVM.OrderHeader.Id);

			if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
			{
				var options = new RefundCreateOptions
				{
					Reason = RefundReasons.RequestedByCustomer,
					PaymentIntent = orderHeader.PaymentIntentId
				};

				var service = new RefundService();
				Refund refund = service.Create(options);

				_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
			}
			else
			{
				_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
			}
			_unitOfWork.Save();
			TempData["Success"] = "Order Cancelled Successfully.";
			return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });

		}

		[ActionName("Details")]
		[HttpPost]
		public IActionResult Details_PAY_NOW()
		{
			orderVM.OrderHeader = _unitOfWork.OrderHeader
				.GetInclud(u => u.Id == orderVM.OrderHeader.Id,"ApplicationUser");
			orderVM.OrderDetail = _unitOfWork.OrderDetail
				.GetAndInclude(u => u.OrderHeaderId == orderVM.OrderHeader.Id, "Product");

			//stripe logic
			var domain = Request.Scheme + "://" + Request.Host.Value + "/"; ;
			var options = new SessionCreateOptions
			{
				SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
				CancelUrl = domain + $"admin/order/details?orderId={orderVM.OrderHeader.Id}",
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
			};

			foreach (var item in orderVM.OrderDetail)
			{
				var sessionLineItem = new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
						Currency = "usd",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = item.Product.Title
						}
					},
					Quantity = item.Count
				};
				options.LineItems.Add(sessionLineItem);
			}


			var service = new SessionService();
			Session session = service.Create(options);
			_unitOfWork.OrderHeader.UpdateStripePaymentID(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);
		}
		public IActionResult PaymentConfirmation(int orderHeaderId)
		{

			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
			if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
			{
				//this is an order by company

				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}


			}


			return View(orderHeaderId);
		}
		#region Json
		public IActionResult GetAll(string status)
        {

            IEnumerable<OrderHeader> ordertList ;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                ordertList = _unitOfWork.OrderHeader.GetAndInclude(agers:"ApplicationUser");
            }
            else
            {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                ordertList = _unitOfWork.OrderHeader.GetAndInclude(o=>o.ApplicationUserId==userId, "ApplicationUser");
            }
            switch (status)
			{
				case "pending":
					ordertList = ordertList.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
					break;
				case "inprocess":
					ordertList = ordertList.Where(u => u.OrderStatus == SD.StatusInProcess);
					break;
				case "completed":
					ordertList = ordertList.Where(u => u.OrderStatus == SD.StatusShipped);
					break;
				case "approved":
					ordertList = ordertList.Where(u => u.OrderStatus == SD.StatusApproved);
					break;
				default:
					break;

			}

			return Json(new { Data = ordertList });

        }
        #endregion
    }
}

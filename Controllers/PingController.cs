using Microsoft.AspNetCore.Mvc;

namespace IotTestApplication.Controllers {
	[Route("api/[controller]")]
	public class PingController : Controller {
		// GET api/ping
		[HttpGet]
		public string Get() {
			return "pong";
		}
	}
}
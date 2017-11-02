using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Gpio;

namespace IotTestApplication.Controllers {
	[Route("api/[controller]")]
	public class GpioController : Controller {
		// GET api/gpio
		[HttpGet]
		public string Get() {
			//var systemInfo = Pi.Info;
			var sb = new StringBuilder();
			foreach (var pin in Pi.Gpio) {
				sb.AppendLine($"Value: {pin.ReadValue()}");
				sb.AppendLine($"Level: {pin.ReadLevel()}");
				sb.AppendLine($"Boolean Value: {pin.Read()}");
				sb.AppendLine($"Mode: {pin.PinMode}");
				sb.AppendLine($"Capabilities: {string.Join(",", pin.Capabilities.Select(x => x.ToString()))}");
				sb.AppendLine($"Header: {pin.Header}");
				sb.AppendLine($"Name: {pin.Name}");
				sb.AppendLine($"Number: {pin.PinNumber}");
				sb.AppendLine($"Pwm Mode: {pin.PwmMode}");
				sb.AppendLine($"Soft Pwm Value: {pin.SoftPwmValue}");
				sb.AppendLine("==============================================================================");
			}
			return sb.ToString();
		}

		// GET api/gpio/5
		[HttpGet("{pinId}")]
		public string Get(int pinId) {
			Console.WriteLine($"Fetching data for pin #{pinId}...");
			var pin = Pi.Gpio[pinId];
			pin.PinMode = GpioPinDriveMode.Input;
			var sb = new StringBuilder();
			sb.AppendLine($"Value: {pin.ReadValue()}");
			sb.AppendLine($"Level: {pin.ReadLevel()}");
			sb.AppendLine($"Boolean Value: {pin.Read()}");
			sb.AppendLine($"Mode: {pin.PinMode}");
			sb.AppendLine($"Capabilities: {string.Join(",", pin.Capabilities.Select(x => x.ToString()))}");
			sb.AppendLine($"Header: {pin.Header}");
			sb.AppendLine($"Name: {pin.Name}");
			sb.AppendLine($"Number: {pin.PinNumber}");
			sb.AppendLine($"Pwm Mode: {pin.PwmMode}");
			sb.AppendLine($"Soft Pwm Value: {pin.SoftPwmValue}");

			return sb.ToString();
		}

		// PUT api/gpio/5
		[HttpPut("{pinId}")]
		public IActionResult Put(int pinId, [FromBody] int value) {
			if (value > 1 || value < 0)
				return BadRequest("Value must be 0 or 1");
			var pin = Pi.Gpio[pinId];
			pin.PinMode = GpioPinDriveMode.Output;
			pin.Write((GpioPinValue) value);

			return NoContent();
		}
	}
}
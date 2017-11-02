using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Gpio;

namespace IotTestApplication.Controllers {
	[Route("api/temperature-readings")]
	public class TemperatureReadingsController : Controller {
		private const int PIN_NUMBER = 1;

		// GET api/temperature-readings
		[HttpGet]
		public string Get() {
			var pin = Pi.Gpio[PIN_NUMBER];
			pin.PinMode = GpioPinDriveMode.Output;
			pin.Write(GpioPinValue.High);
			Thread.Sleep(TimeSpan.FromMilliseconds(50));
			pin.Write(GpioPinValue.Low);
			Thread.Sleep(TimeSpan.FromMilliseconds(20));

			pin.PinMode = GpioPinDriveMode.Input;
			pin.InputPullMode = GpioPinResistorPullMode.PullUp;

			// collect data into an array
			var data = collectInput();
			Console.WriteLine($"Collected input bits: {data}");

			// parse lenghts of all data pull up periods
			var pullUpLengths = parseDataPullUpLengths(data);
			Console.WriteLine($"Collected pull-up lenghts: {pullUpLengths}");

			// if bit count mismatch, return error (4 byte data + 1 byte checksum)
			if (pullUpLengths.Count != 40)
				return "Invalid sensor data, bit count mismatch detected.";

			// calculate bits from lengths of the pull up periods
			var bits = calculateBits(pullUpLengths);

			// we have the bits, calculate bytes
			var bytes = bitsToBytes(bits);

			// calculate checksum and check
			var checksum = calculateChecksum(bytes);
			if (bytes[4] != checksum)
				return "Invalid result checksum!";

			// we have valid data, return it
			var temperature = bytes[2];
			var humidity = bytes[0];

			return $"Temperature: {temperature}, humidity: {humidity}";
		}

		private int calculateChecksum(List<int> bytes) {
			return bytes[0] + bytes[1] + bytes[2] + bytes[3] & 255;
		}

		private List<int> bitsToBytes(IReadOnlyList<bool> bits) {
			var bytes = new List<int>();
			var @byte = 0;

			for (var i = 0; i < bits.Count; i++) {
				@byte = @byte << 1;
				@byte = bits[i] ? @byte | 1 : @byte | 0;
				if ((i + 1) % 8 == 0) {
					bytes.Add(@byte);
					@byte = 0;
				}
			}
			return bytes;
		}

		private List<bool> calculateBits(List<int> pullUpLengths) {
			// find shortest and longest period
			var shortestPullUp = pullUpLengths.Min();
			var longestPullUp = pullUpLengths.Max();

			// use the halfway to determine whether the period is long or short
			var halfway = shortestPullUp + (longestPullUp - shortestPullUp) / 2;
			var bits = new List<bool>();

			foreach (var length in pullUpLengths) {
				var bit = length > halfway;
				bits.Add(bit);
			}

			return bits;
		}

		private List<int> parseDataPullUpLengths(byte[] data) {
			const int STATE_INIT_PULL_DOWN = 1;
			const int STATE_INIT_PULL_UP = 2;
			const int STATE_DATA_FIRST_PULL_DOWN = 3;
			const int STATE_DATA_PULL_UP = 4;
			const int STATE_DATA_PULL_DOWN = 5;

			var state = STATE_INIT_PULL_DOWN;

			var lengths = new List<int>(); // will contain the lengths of data pull up periods
			var currentLength = 0; // will contain the lengths of the previous period

			foreach (var bit in data) {
				currentLength++;

				switch (state) {
					case STATE_INIT_PULL_DOWN:
						if (bit == (int) GpioPinValue.Low)
							// we got the initial pull down
							state = STATE_INIT_PULL_UP;
						continue;
					case STATE_INIT_PULL_UP:
						if (bit == (int) GpioPinValue.High)
							// we got the initial pull up
							state = STATE_DATA_FIRST_PULL_DOWN;
						continue;
					case STATE_DATA_FIRST_PULL_DOWN:
						if (bit == (int) GpioPinValue.Low)
							// we have the initial pull down, the next will be the data pull up
							state = STATE_DATA_PULL_UP;
						continue;
					case STATE_DATA_PULL_UP:
						if (bit == (int) GpioPinValue.High) {
							// data pulled up, the length of this pull up will determine whether it is 0 or 1
							currentLength = 0;
							state = STATE_DATA_PULL_DOWN;
						}
						continue;
					case STATE_DATA_PULL_DOWN:
						if (bit == (int) GpioPinValue.Low) {
							// pulled down, we store the length of the previous pull up period
							lengths.Add(currentLength);
							state = STATE_DATA_PULL_UP;
						}
						continue;
				}
			}

			return lengths;
		}

		private byte[] collectInput() {
			var unchangedCount = 0;
			var maxUnchangedCount = 100;
			var last = -1;
			var data = new List<byte>();

			while (true) {
				var current = Pi.Gpio[PIN_NUMBER].ReadValue();
				data.Add((byte) current);
				if (last != (int) current) {
					unchangedCount = 0;
					last = (int) current;
				} else {
					unchangedCount++;
					if (unchangedCount > maxUnchangedCount)
						break;
				}
			}
			return data.ToArray();
		}
	}
}
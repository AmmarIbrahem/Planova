using System.Diagnostics.Metrics;

namespace Planova.Infrastructure.Observability
{
	public sealed class PlanovaMetrics
	{
		private readonly Counter<long> _bookingAttempts;
		private readonly Counter<long> _bookingFailures;
		private readonly Counter<long> _bookingSuccess;
		private readonly Histogram<double> _bookingDuration;

		public static readonly string MeterName = "Planova";

		public PlanovaMetrics(IMeterFactory factory)
		{
			var meter = factory.Create(MeterName);

			_bookingAttempts = meter.CreateCounter<long>(
				"planova.bookings.attempts",
				description: "Total booking attempts");

			_bookingSuccess = meter.CreateCounter<long>(
				"planova.bookings.success",
				unit: "{success}",
				description: "Booking success");

			_bookingFailures = meter.CreateCounter<long>(
				"planova.bookings.failures",
				unit: "{failure}",
				description: "Booking failures by reason");

			_bookingDuration = meter.CreateHistogram<double>(
				"planova.bookings.duration",
				unit: "ms",
				description: "Booking handler execution time");
		}

		public void RecordBookingAttempt() => _bookingAttempts.Add(1);
		public void RecordBookingSuccess() => _bookingSuccess.Add(1);
		public void RecordBookingFailure(string reason) => _bookingFailures.Add(1, new KeyValuePair<string, object?>("reason", reason));
		public void RecordBookingDuration(double ms) => _bookingDuration.Record(ms);
	}
}

namespace Planova.Application.Common
{
	public class Result
	{
		public bool IsSuccess { get; set; }
		public string? ErrorMessage { get; set; }
		public object? Data { get; set; }

		// Constructor for success
		public Result(object? data = null)
		{
			IsSuccess = true;
			Data = data;
		}

		// Constructor for failure
		public Result(string errorMessage)
		{
			IsSuccess = false;
			ErrorMessage = errorMessage;
		}

		// Factory methods for ease of use
		public static Result Success(object? data = null) => new Result(data);
		public static Result Failure(string errorMessage) => new Result(errorMessage);
	}
}

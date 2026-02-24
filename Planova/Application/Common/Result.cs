namespace Planova.Application.Common
{
	public class Result
	{
		public bool IsSuccess { get; set; }
		public string? ErrorMessage { get; set; }
		public object? Data { get; set; }

		public Result(object? data = null)
		{
			IsSuccess = true;
			Data = data;
		}

		public Result(string errorMessage)
		{
			IsSuccess = false;
			ErrorMessage = errorMessage;
		}

		public static Result Success(object? data = null) => new Result(data);
		public static Result Failure(string errorMessage) => new Result(errorMessage);
	}
}

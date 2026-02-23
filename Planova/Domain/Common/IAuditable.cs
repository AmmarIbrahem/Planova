namespace Planova.Domain.Common
{
	public interface IAuditable
	{
		DateTime CreatedAt { get; }
		DateTime UpdatedAt { get; }
		string CreatedBy { get; }
		string UpdatedBy { get; }
		int IsDeleted { get; }
	}
}

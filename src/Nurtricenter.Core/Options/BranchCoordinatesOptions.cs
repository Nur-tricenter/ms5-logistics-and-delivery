namespace Nurtricenter.Core.Options;

public sealed class BranchCoordinatesOptions
{
	public const string SectionName = "branchCoordinates";

	public required double Latitude { get; init; }
	public required double Longitude { get; init; }
}

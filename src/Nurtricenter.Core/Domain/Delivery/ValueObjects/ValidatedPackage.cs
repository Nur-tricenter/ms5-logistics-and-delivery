#pragma warning disable CS8618 // Parameterless constructor for serialization

namespace Nurtricenter.Core.Domain.Delivery.ValueObjects;

using Joseco.DDD.Core.Results;

public sealed record ValidatedPackage
{
    public string PackageId { get; }
    public string PatientId { get; }
    public string PatientName { get; }
    public string LabelData { get; }

    public ValidatedPackage(string packageId, string patientId, string patientName, string labelData)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            throw new DomainException(
                new Error(
                    "ValidatedPackage.EmptyPackageId",
                    "Package ID cannot be empty.",
                    ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(patientId))
            throw new DomainException(
                new Error(
                    "ValidatedPackage.EmptyPatientId",
                    "Patient ID cannot be empty.",
                    ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(patientName))
            throw new DomainException(
                new Error(
                    "ValidatedPackage.EmptyPatientName",
                    "Patient name cannot be empty.",
                    ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(labelData))
            throw new DomainException(
                new Error(
                    "ValidatedPackage.EmptyLabelData",
                    "Label data cannot be empty.",
                    ErrorType.Validation));

        PackageId = packageId;
        PatientId = patientId;
        PatientName = patientName;
        LabelData = labelData;
    }

    private ValidatedPackage() { }
}

namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents an immutable customer destination model.
    /// </summary>
    /// <param name="Id">The customer identifier.</param>
    /// <param name="FullName">The customer full name.</param>
    public sealed record CustomerRecordDto(int Id, string FullName);
}

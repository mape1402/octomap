namespace OctoMap
{
    /// <summary>
    /// Ignores the annotated destination member during mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class IgnoreMapAttribute : Attribute
    {
    }
}

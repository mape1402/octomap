namespace OctoMap
{
    /// <summary>
    /// Marks the constructor OctoMap should prefer for constructor mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class MapConstructorAttribute : Attribute
    {
    }
}

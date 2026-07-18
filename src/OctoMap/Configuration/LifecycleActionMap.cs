namespace OctoMap.Configuration
{
    /// <summary>
    /// Represents a configured lifecycle action.
    /// </summary>
    internal sealed class LifecycleActionMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LifecycleActionMap"/> class.
        /// </summary>
        /// <param name="timing">The action timing.</param>
        /// <param name="inlineActionId">The configured inline action identifier.</param>
        /// <param name="actionType">The configured service action type.</param>
        public LifecycleActionMap(LifecycleActionTiming timing, int? inlineActionId, Type actionType)
        {
            Timing = timing;
            InlineActionId = inlineActionId;
            ActionType = actionType;
        }

        /// <summary>
        /// Gets the action timing.
        /// </summary>
        public LifecycleActionTiming Timing { get; }

        /// <summary>
        /// Gets the configured inline action.
        /// </summary>
        public int? InlineActionId { get; }

        /// <summary>
        /// Gets the configured service action type.
        /// </summary>
        public Type ActionType { get; }
    }

    /// <summary>
    /// Represents when a lifecycle action executes.
    /// </summary>
    public enum LifecycleActionTiming
    {
        /// <summary>
        /// Executes before member assignment.
        /// </summary>
        Before,

        /// <summary>
        /// Executes after member assignment.
        /// </summary>
        After
    }
}

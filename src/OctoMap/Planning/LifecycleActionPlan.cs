using OctoMap.Configuration;

namespace OctoMap.Planning
{
    /// <summary>
    /// Represents a planned lifecycle action.
    /// </summary>
    public sealed class LifecycleActionPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LifecycleActionPlan"/> class.
        /// </summary>
        /// <param name="timing">The action timing.</param>
        /// <param name="inlineActionId">The configured inline action identifier.</param>
        /// <param name="actionType">The configured service action type.</param>
        public LifecycleActionPlan(LifecycleActionTiming timing, int? inlineActionId, Type actionType)
        {
            Timing = timing;
            InlineActionId = inlineActionId;
            ActionType = actionType;
        }

        /// <summary>
        /// Gets the action timing.
        /// </summary>
        internal LifecycleActionTiming Timing { get; }

        /// <summary>
        /// Gets the configured inline action.
        /// </summary>
        public int? InlineActionId { get; }

        /// <summary>
        /// Gets the configured service action type.
        /// </summary>
        public Type ActionType { get; }
    }
}

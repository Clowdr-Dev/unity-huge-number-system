using System;

namespace HugeNumberSystem.Numerics.Authoring
{
    public enum HugeNumberAuthoringIssueSeverity
    {
        Warning,
        Error,
    }

    public readonly struct HugeNumberAuthoringIssue
    {
        public HugeNumberAuthoringIssue(HugeNumberAuthoringIssueSeverity severity, string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            Severity = severity;
            Message = message;
        }

        public HugeNumberAuthoringIssueSeverity Severity { get; }
        public string Message { get; }
    }
}

namespace Common;

public interface IReasonForRemovalLookup
{
    bool CanRemovalReasonBeOverridden(string? reasonForRemoval);
}

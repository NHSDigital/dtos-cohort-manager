namespace Common;

public class ReasonForRemovalLookup : IReasonForRemovalLookup
{

    private readonly List<string> NonOverridableRFRs;
    private readonly List<string> OverridableRFRs;
    public ReasonForRemovalLookup()
    {
        NonOverridableRFRs = new List<string>
        {
            "AFL",
            "AFN",
            "DEA",
            "LDN",
            "SDL",
            "SDN",
            "TRA"
        };
    }
    public bool CanRemovalReasonBeOverridden(string? reasonForRemoval)
    {
        if(reasonForRemoval is null)
        {
            return true;
        }

        if (NonOverridableRFRs.Contains(reasonForRemoval.ToUpper()))
        {
            return false;
        }

        return true;
    }

}

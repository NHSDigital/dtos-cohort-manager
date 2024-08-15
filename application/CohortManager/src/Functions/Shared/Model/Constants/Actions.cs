namespace Model;

using System.Reflection;

public class Actions
{
    public const string New = "New";
    public const string Amended = "Amended";
    public const string Removed = "Removed";


    /// <summary>
    /// gets all the actions and tries to parse the Given action to the available action
    /// </summary>
    /// <param name="actionIn"></param>
    /// <param name="ActionOut"></param>
    /// <returns></returns>
    public static bool TryParse(string actionIn, out string ActionOut)
    {
        ActionOut = null;
        if (string.IsNullOrWhiteSpace(actionIn))
        {
            return false;
        }

        //get all public static actions in the actions class
        var actions = typeof(Actions).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        foreach (var action in actions)
        {
            if (action.GetValue(null) is string value && value.Equals(actionIn, StringComparison.OrdinalIgnoreCase))
            {
                ActionOut = value;
                return true;
            }
        }
        return false;
    }
}

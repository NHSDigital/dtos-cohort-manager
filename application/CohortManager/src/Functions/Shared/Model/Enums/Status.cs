namespace Model.Enums;


/// <summary>
/// Enum representing the death status,
/// either formal (a death certificate has been issued)
/// or informal (no death certificate has been issued)
/// </summary>
public enum Status : short
{
    Informal = 1,
    Formal = 2
}

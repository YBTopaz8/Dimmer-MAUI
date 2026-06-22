namespace Dimmer.Utils;

[AttributeUsage(AttributeTargets.All)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class PreserveAttribute : Attribute
{
    public bool AllMembers;
    public bool Conditional;
}
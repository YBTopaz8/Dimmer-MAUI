using Dimmer.Utils.CustomShellUtils.Enums;

namespace Dimmer.Utils.CustomShellUtils.Models;
public class TransitionRoot : Transition
{
    public PageType AbovePage { get; set; } = Enums.PageType.CurrentPage;
}

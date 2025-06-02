using Dimmer.Utils.CustomShellUtils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils.CustomShellUtils.Models;
public class TransitionRoot : Transition
{
    public PageType AbovePage { get; set; } = Enums.PageType.CurrentPage;
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils.CustomShellUtils.Models;
public class Transitions
{
    public TransitionRoot Root { get; set; } = new TransitionRoot();
    public Transition Push { get; set; } = new Transition();
    public Transition Pop { get; set; } = new Transition();
}
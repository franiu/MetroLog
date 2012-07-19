﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetroLog.Layouts;

namespace MetroLog.Targets
{
    public abstract class AsyncTarget : Target
    {
        protected AsyncTarget(Layout layout)
            : base(layout)
        {
        }

        protected internal abstract Task WriteAsync(LogEventInfo entry);
    }
}
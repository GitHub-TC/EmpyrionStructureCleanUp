using EmpyrionAPIDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmpyrionStructureCleanUp
{

    public class Configuration
    {
        public int OnlyCleanIfOlderThan { get; set; } = 14;
        public string MoveToDirectory { get; set; } = @"..\..\..\..\..\Backup\CleanUp";
        public bool DeletePermanent { get; set; } = false;
        public bool CleanOnStartUp { get; set; } = false;
    }
}

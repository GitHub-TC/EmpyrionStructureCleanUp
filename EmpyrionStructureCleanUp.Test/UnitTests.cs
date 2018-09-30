using System;
using System.IO;
using System.Linq;
using Eleon.Modding;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmpyrionStructureCleanUp.Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestMethodTranslate()
        {
            System.Collections.Generic.List<GlobalStructureInfo> AllStructures = new System.Collections.Generic.List<GlobalStructureInfo>();

            var UnusedObjects = CleanUp.GetUnusedObjects(
                Path.Combine(@"C:\steamcmd\empyrion\Saves\Games\Server\Shared"),
                AllStructures).ToArray();

            var PossibleCleanUpObjects = UnusedObjects.Where(O => (DateTime.Now - O.LastAccess).TotalDays > 14).ToArray();

            var usedTypes = AllStructures.Distinct(new StructureTypeEqualityComparer());
            var Result =
                usedTypes.Aggregate("", (L, T) => L + T.type + ": " + AllStructures.Count(S => S.type == T.type)) + "\n" +
                $"Unused:{UnusedObjects.Length} ({UnusedObjects.Aggregate(0L, (S, O) => S + O.GetSize()) / (1024 * 1024):N2}MB) possible CleanUp:{PossibleCleanUpObjects.Length} ({PossibleCleanUpObjects.Aggregate(0L, (S, O) => S + O.GetSize()) / (1024 * 1024):N2}MB)"
            ;
        }
    }
}

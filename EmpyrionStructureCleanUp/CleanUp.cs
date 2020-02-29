using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eleon.Modding;

namespace EmpyrionStructureCleanUp
{
    public class StructureTypeEqualityComparer : IEqualityComparer<GlobalStructureInfo>
    {
        public bool Equals(GlobalStructureInfo x, GlobalStructureInfo y)
        {
            return x.type == y.type;
        }

        public int GetHashCode(GlobalStructureInfo obj)
        {
            return obj.type;
        }
    }

    public class CleanUp
    {
        public class CleanUpStucture
        {
            private long mSize;

            public string DataDirectory { get; set; }
            public string InfoFile { get; set; }

            public long GetSize()
            {
                if(mSize == 0) mSize = Directory.GetFiles(DataDirectory, "*.*", SearchOption.AllDirectories)
                    .Aggregate(InfoFile == null ? 0L : new FileInfo(InfoFile).Length, (S, F) => S + new FileInfo(F).Length);
                return mSize;
            }

            public void Delete()
            {
                if(InfoFile != null) File.Delete(InfoFile);
                Directory.Delete(DataDirectory, true);
            }

            public DateTime LastAccess
            {
                get {
                    var last = Directory.GetFiles(DataDirectory, "*.*", SearchOption.AllDirectories)
                        .Aggregate(Directory.GetLastAccessTime(DataDirectory), (T, F) => File.GetLastAccessTime(F) < T ? File.GetLastAccessTime(F) : T);

                    return InfoFile == null ? last : (File.GetLastAccessTime(InfoFile) < last ? File.GetLastAccessTime(InfoFile) : last);
                }
            }

            public void MoveTo(string aMoveToDirectory)
            {
                Directory.CreateDirectory(aMoveToDirectory);
                if (InfoFile != null) File.Move(InfoFile, Path.Combine(aMoveToDirectory, Path.GetFileName(InfoFile)));
                Directory.Move(DataDirectory, Path.Combine(aMoveToDirectory, Path.GetFileName(DataDirectory)));
            }
        }

        public static IEnumerable<CleanUpStucture> GetUnusedObjects(string aSharedPath, List<GlobalStructureInfo> aAllStructures)
        {
            var AllStructuresDict = aAllStructures.ToDictionary(S => S.id, S => S);

            return Directory.GetDirectories(aSharedPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(D => {
                        var NumberPos = D.LastIndexOf('_');
                        return int.TryParse(D.Substring(NumberPos + 1), out int Id) && !AllStructuresDict.Keys.Contains(Id);
                    }
                )
                .Select(D => new CleanUpStucture() {
                    DataDirectory     = D,
                    InfoFile = FindInfoFileFor(aSharedPath, D)
                });
        }

        private static string FindInfoFileFor(string aSharedPath, string aStructureDir)
        {
            var InfoFilename = Path.Combine(aSharedPath, Path.GetFileName(aStructureDir) + ".txt");
            return File.Exists(InfoFilename) ? InfoFilename : null;
        }
    }
}

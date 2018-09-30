using System;
using Eleon.Modding;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using EmpyrionAPIDefinitions;

namespace EmpyrionStructureCleanUp
{
    public class StructureCleanUp
    {
        public Configuration Configuration { get; set; } = new Configuration();
        public static Action<string, LogLevel> LogDB { get; set; }

        private static void log(string aText, LogLevel aLevel)
        {
            LogDB?.Invoke(aText, aLevel);
        }

        public void SaveDB(string DBFileName)
        {
            var serializer = new XmlSerializer(typeof(StructureCleanUp));
            Directory.CreateDirectory(Path.GetDirectoryName(DBFileName));
            using (var writer = XmlWriter.Create(DBFileName, new XmlWriterSettings() { Indent = true, IndentChars = "  " }))
            {
                serializer.Serialize(writer, this);
            }
        }

        public static StructureCleanUp ReadDB(string DBFileName)
        {
            if (!File.Exists(DBFileName))
            {
                log($"StructureCleanUpDB ReadDB not found '{DBFileName}'", LogLevel.Error);
                return new StructureCleanUp();
            }

            try
            {
                log($"StructureCleanUpDB ReadDB load '{DBFileName}'", LogLevel.Message);
                var serializer = new XmlSerializer(typeof(StructureCleanUp));
                using (var reader = XmlReader.Create(DBFileName))
                {
                    return (StructureCleanUp)serializer.Deserialize(reader);
                }
            }
            catch(Exception Error)
            {
                log("StructureCleanUpDB ReadDB" + Error.ToString(), LogLevel.Error);
                return new StructureCleanUp();
            }
        }

    }
}

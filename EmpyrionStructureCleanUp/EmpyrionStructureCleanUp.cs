using System;
using Eleon.Modding;
using EmpyrionAPITools;
using System.Collections.Generic;
using EmpyrionAPIDefinitions;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace EmpyrionStructureCleanUp
{
    public static class Extensions{

        public static T GetAttribute<T>(this Assembly aAssembly)
        {
            return aAssembly.GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();
        }

        static Regex GetCommand = new Regex(@"(?<cmd>(\w|\/|\s)+)");

        public static string MsgString(this ChatCommand aCommand)
        {
            var CmdString = GetCommand.Match(aCommand.invocationPattern).Groups["cmd"]?.Value ?? aCommand.invocationPattern;
            return $"[c][ff00ff]{CmdString}[-][/c]{aCommand.paramNames.Aggregate(" ", (S, P) => S + $"<[c][00ff00]{P}[-][/c]> ")}: {aCommand.description}";
        }

    }

    public partial class EmpyrionStructureCleanUp : SimpleMod
    {
        public ModGameAPI GameAPI { get; set; }
        public StructureCleanUp StructureCleanUpsDB { get; set; }
        public string StructureCleanUpsDBFilename { get; set; }

        FileSystemWatcher DBFileChangedWatcher;
        private string mCalcHeadline;
        private string mCalcBody;
        private string mCleanUpStatus;
        private CleanUp.CleanUpStucture[] mPossibleCleanUpObjects;

        enum SubCommand
        {
            Help,
            List,
            Calc,
            CleanUp
        }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;
            verbose = true;
            this.LogLevel = LogLevel.Error;

            log($"**HandleEmpyrionStructureCleanUp loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

            InitializeDB();
            InitializeDBFileWatcher();

            ChatCommands.Add(new ChatCommand(@"/struct help",       (I, A) => ExecCommand(SubCommand.Help,     I, A), "Show the help"                    , PermissionType.Admin));
            ChatCommands.Add(new ChatCommand(@"/struct list",       (I, A) => ExecCommand(SubCommand.List,     I, A), "List all structures"              , PermissionType.Admin));
            ChatCommands.Add(new ChatCommand(@"/struct calc",       (I, A) => ExecCommand(SubCommand.Calc,     I, A), "Calc all structures again"        , PermissionType.Admin));
            ChatCommands.Add(new ChatCommand(@"/struct cleanup",    (I, A) => ExecCommand(SubCommand.CleanUp,  I, A), "CleanUp old and unsued structures", PermissionType.Admin));

            CalcStructures(() => { if (StructureCleanUpsDB.Configuration.CleanOnStartUp) CleanUpStructuresWorker(); });
        }

        private void InitializeDBFileWatcher()
        {
            DBFileChangedWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(StructureCleanUpsDBFilename),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(StructureCleanUpsDBFilename)
            };
            DBFileChangedWatcher.Changed += (s, e) => StructureCleanUpsDB = StructureCleanUp.ReadDB(StructureCleanUpsDBFilename);
            DBFileChangedWatcher.EnableRaisingEvents = true;
        }

        private void InitializeDB()
        {
            StructureCleanUpsDBFilename = Path.Combine(EmpyrionConfiguration.ProgramPath, @"Saves\Games\" + EmpyrionConfiguration.DedicatedYaml.SaveGameName + @"\Mods\EmpyrionStructureCleanUp\StructureCleanUpsDB.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(StructureCleanUpsDBFilename));

            StructureCleanUp.LogDB = log;
            StructureCleanUpsDB = StructureCleanUp.ReadDB(StructureCleanUpsDBFilename);
            StructureCleanUpsDB.SaveDB(StructureCleanUpsDBFilename);
        }


        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private void ExecCommand(SubCommand aCommand, ChatInfo info, Dictionary<string, string> args)
        {
            log($"**HandleEmpyrionStructureCleanUp {info.type}#{aCommand}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}", LogLevel.Message);

            if (info.type != (byte)ChatType.Faction) return;

            switch (aCommand)
            {
                case SubCommand.Help    : DisplayHelp               (info.playerId); break;
                case SubCommand.List    : ListStructures            (info.playerId); break;
                case SubCommand.Calc    : CallCalcStructures        (info.playerId); break;
                case SubCommand.CleanUp : CleanUpStructures         (info.playerId); break;
            }
        }

        private void CallCalcStructures(int aPlayerId)
        {
            Request_Player_Info(aPlayerId.ToId(), P =>
            {
                ShowDialog(aPlayerId, P, "CleanUp", "calculate running, call '/struct list' in a few seconds");
            });

            CalcStructures(null);
        }

        private void CalcStructures(Action aExecAfter)
        {
            var Timer     = new Stopwatch();
            var FullTimer = new Stopwatch();
            Timer    .Start();
            FullTimer.Start();

            Request_GlobalStructure_List(G => {
                Timer.Stop();

                new Thread(() => {
                    var AllStructures = G.globalStructures.Aggregate(new List<GlobalStructureInfo>(), (L, GPL) => { L.AddRange(GPL.Value); return L; });

                    File.WriteAllText(Path.Combine(EmpyrionConfiguration.ProgramPath, @"Saves\Games\" + EmpyrionConfiguration.DedicatedYaml.SaveGameName + @"\Mods\EmpyrionStructureCleanUp\StructureCleanUpsDB.txt"),
                            AllStructures.Aggregate("", (L, S) => L + $"{S.id} {(EntityType)S.type} {S.name}" + "\n")
                        );

                    var UnusedObjects = CleanUp.GetUnusedObjects(
                        Path.Combine(EmpyrionConfiguration.ProgramPath, @"Saves\Games\" + EmpyrionConfiguration.DedicatedYaml.SaveGameName + @"\Shared"),
                        AllStructures).ToArray();

                    mPossibleCleanUpObjects = UnusedObjects.Where(O => (DateTime.Now - O.LastAccess).TotalDays > StructureCleanUpsDB.Configuration.OnlyCleanIfOlderThan).ToArray();

                    var usedTypes = AllStructures.Distinct(new StructureTypeEqualityComparer());
                    mCalcBody     = usedTypes.Aggregate("", (L, T) => L + (EntityType)T.type + ": " + AllStructures.Count(S => S.type == T.type) + "\n") +
                                    $"\nUnused:{UnusedObjects.Length} ({UnusedObjects.Aggregate(0L, (S, O) => S + O.GetSize()) / (1024 * 1024):N2}MB) possible CleanUp:{mPossibleCleanUpObjects.Length} ({mPossibleCleanUpObjects.Aggregate(0L, (S, O) => S + O.GetSize()) / (1024 * 1024):N2}MB)";
                    FullTimer.Stop();
                    mCalcHeadline = $"Empyrion Structures (Playfields #{G.globalStructures.Count} Structures #{G.globalStructures.Aggregate(0, (c, p) => c + p.Value.Count)} load {Timer.Elapsed.TotalMilliseconds:N2}ms) total: {FullTimer.Elapsed.TotalSeconds:N2}s";

                    if (aExecAfter != null) aExecAfter();
                }).Start();
            });
        }

        private void ListStructures(int aPlayerId)
        {
            Request_Player_Info(aPlayerId.ToId(), P =>
            {
                ShowDialog(aPlayerId, P, mCalcHeadline, mCalcBody + "\n" + mCleanUpStatus);
            });
        }

        private void CleanUpStructures(int aPlayerId)
        {
            Request_Player_Info(aPlayerId.ToId(), P =>
            {
                ShowDialog(aPlayerId, P, "CleanUp", mPossibleCleanUpObjects == null ? "wait for calc" : "running, call '/struct list' in a few seconds");
            });

            if (mPossibleCleanUpObjects == null) return;

            new Thread(() =>
            {
                CleanUpStructuresWorker();
                CalcStructures         (null);
            }).Start();
        }

        private void CleanUpStructuresWorker()
        {
            var Timer = new Stopwatch();
            Timer.Start();

            var MoveToDirectory = Path.Combine(Path.GetDirectoryName(StructureCleanUpsDBFilename), StructureCleanUpsDB.Configuration.MoveToDirectory);
            List<string> Errors = null;
            
            Array.ForEach(mPossibleCleanUpObjects, O => {
                try
                {
                    if (StructureCleanUpsDB.Configuration.DeletePermanent) O.Delete();
                    else                                                   O.MoveTo(MoveToDirectory);
                }
                catch (Exception Error)
                {
                    if(Errors == null) Errors = new List<string>();
                    Errors.Add($"{O.InfoFile} {O.DataDirectory}: " + Error.Message);
                }
            });
            Timer.Stop();

            if (Errors != null) log(Errors.Aggregate("", (S, E) => S + E + "\n"), LogLevel.Error);

            mCleanUpStatus = $"ExecCleanUp: ({(Errors == null ? "success" : Errors.Count + " errors")} total: {Timer.Elapsed.TotalSeconds:N2}s";
        }

        private void LogError(string aPrefix, ErrorInfo aError)
        {
            log($"{aPrefix} Error: {aError.errorType} {aError.ToString()}", LogLevel.Error);
        }

        void ShowDialog(int aPlayerId, PlayerInfo aPlayer, string aTitle, string aMessage)
        {
            Request_ShowDialog_SinglePlayer(new DialogBoxData()
            {
                Id      = aPlayerId,
                MsgText = $"{aTitle}: [c][ffffff]{aPlayer.playerName}[-][/c] with permission [c][ffffff]{(PermissionType)aPlayer.permission}[-][/c]\n\n" + aMessage,
            });
        }

        private void DisplayHelp(int aPlayerId)
        {
            Request_Player_Info(aPlayerId.ToId(), (P) =>
            {
                var CurrentAssembly = Assembly.GetAssembly(this.GetType());
                //[c][hexid][-][/c]    [c][019245]test[-][/c].

                ShowDialog(aPlayerId, P, "Commands",
                    String.Join("\n", GetChatCommandsForPermissionLevel((PermissionType)P.permission).Select(C => C.MsgString()).ToArray()) +
                    $"\n\n[c][c0c0c0]{CurrentAssembly.GetAttribute<AssemblyTitleAttribute>()?.Title} by {CurrentAssembly.GetAttribute<AssemblyCompanyAttribute>()?.Company} Version:{CurrentAssembly.GetAttribute<AssemblyFileVersionAttribute>()?.Version}[-][/c]"
                    );
            });
        }

    }
}

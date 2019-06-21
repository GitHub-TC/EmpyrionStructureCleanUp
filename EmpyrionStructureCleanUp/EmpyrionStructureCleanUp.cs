using Eleon.Modding;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
using EmpyrionNetAPIDefinitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

    public partial class EmpyrionStructureCleanUp : EmpyrionModBase
    {
        public ModGameAPI GameAPI { get; set; }
        public ConfigurationManager<Configuration> Configuration { get; set; }

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

        public EmpyrionStructureCleanUp()
        {
            EmpyrionConfiguration.ModName = "EmpyrionStructureCleanUp";
        }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;

            log($"**HandleEmpyrionStructureCleanUp loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

            InitializeDB();
            LogLevel = Configuration.Current.LogLevel;
            ChatCommandManager.CommandPrefix = Configuration.Current.ChatCommandPrefix;

            ChatCommands.Add(new ChatCommand(@"struct help",       (I, A) => ExecCommand(SubCommand.Help,     I, A), "Show the help"                    , PermissionType.Admin));
            ChatCommands.Add(new ChatCommand(@"struct list",       (I, A) => ExecCommand(SubCommand.List,     I, A), "List all structures"              , PermissionType.Admin));
            ChatCommands.Add(new ChatCommand(@"struct calc",       (I, A) => ExecCommand(SubCommand.Calc,     I, A), "Calc all structures again"        , PermissionType.Admin));
            ChatCommands.Add(new ChatCommand(@"struct cleanup",    (I, A) => ExecCommand(SubCommand.CleanUp,  I, A), "CleanUp old and unsued structures", PermissionType.Admin));

            new Thread(() => CalcStructures(() => { if (Configuration.Current.CleanOnStartUp) CleanUpStructuresWorker(); }).Wait())
                .Start();
        }

        private void InitializeDB()
        {
            ConfigurationManager<Configuration>.Log = log;
            Configuration = new ConfigurationManager<Configuration>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "StructureCleanUpSettings.json")
            };

            Configuration.Load();
            Configuration.Save();
        }


        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private async Task ExecCommand(SubCommand aCommand, ChatInfo info, Dictionary<string, string> args)
        {
            log($"**HandleEmpyrionStructureCleanUp {info.type}#{aCommand}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}", LogLevel.Message);

            if (info.type != (byte)ChatType.Faction) return;

            switch (aCommand)
            {
                case SubCommand.Help    : await DisplayHelp               (info.playerId, ""); break;
                case SubCommand.List    : await ListStructures            (info.playerId); break;
                case SubCommand.Calc    : await CallCalcStructures        (info.playerId); break;
                case SubCommand.CleanUp : await CleanUpStructures         (info.playerId); break;
            }
        }

        private async Task CallCalcStructures(int aPlayerId)
        {
            var P = await Request_Player_Info(aPlayerId.ToId());
            await ShowDialog(aPlayerId, P, "CleanUp", "calculate running, call '/struct list' in a few seconds");

            await CalcStructures(null);
        }

        private async Task CalcStructures(Action aExecAfter)
        {
            var Timer     = new Stopwatch();
            var FullTimer = new Stopwatch();
            Timer    .Start();
            FullTimer.Start();

            var G = await Request_GlobalStructure_List();
            Timer.Stop();

            new Thread(() => {
                var AllStructures = G.globalStructures.Aggregate(new List<GlobalStructureInfo>(), (L, GPL) => { L.AddRange(GPL.Value); return L; });

                File.WriteAllText(Path.Combine(EmpyrionConfiguration.ProgramPath, @"Saves\Games\" + EmpyrionConfiguration.DedicatedYaml.SaveGameName + @"\Mods\EmpyrionStructureCleanUp\StructureCleanUpsDB.txt"),
                        AllStructures.Aggregate("", (L, S) => L + $"{S.id} {(EntityType)S.type} {S.name}" + "\n")
                    );

                var UnusedObjects = CleanUp.GetUnusedObjects(
                    Path.Combine(EmpyrionConfiguration.ProgramPath, @"Saves\Games\" + EmpyrionConfiguration.DedicatedYaml.SaveGameName + @"\Shared"),
                    AllStructures).ToArray();

                mPossibleCleanUpObjects = UnusedObjects.Where(O => (DateTime.Now - O.LastAccess).TotalDays > Configuration.Current.OnlyCleanIfOlderThan).ToArray();

                var usedTypes = AllStructures.Distinct(new StructureTypeEqualityComparer());
                mCalcBody     = usedTypes.Aggregate("", (L, T) => L + (EntityType)T.type + ": " + AllStructures.Count(S => S.type == T.type) + "\n") +
                                $"\nUnused:{UnusedObjects.Length} ({UnusedObjects.Aggregate(0L, (S, O) => S + O.GetSize()) / (1024 * 1024):N2}MB) possible CleanUp:{mPossibleCleanUpObjects.Length} ({mPossibleCleanUpObjects.Aggregate(0L, (S, O) => S + O.GetSize()) / (1024 * 1024):N2}MB)";
                FullTimer.Stop();
                mCalcHeadline = $"Empyrion Structures (Playfields #{G.globalStructures.Count} Structures #{G.globalStructures.Aggregate(0, (c, p) => c + p.Value.Count)} load {Timer.Elapsed.TotalMilliseconds:N2}ms) total: {FullTimer.Elapsed.TotalSeconds:N2}s";

                if (aExecAfter != null) aExecAfter();
            }).Start();
        }

        private async Task ListStructures(int aPlayerId)
        {
            var P = await Request_Player_Info(aPlayerId.ToId());
            await ShowDialog(aPlayerId, P, mCalcHeadline, mCalcBody + "\n" + mCleanUpStatus);
        }

        private async Task CleanUpStructures(int aPlayerId)
        {
            var P = await Request_Player_Info(aPlayerId.ToId());
            await ShowDialog(aPlayerId, P, "CleanUp", mPossibleCleanUpObjects == null ? "wait for calc" : "running, call '/struct list' in a few seconds");

            if (mPossibleCleanUpObjects == null) return;

            new Thread(async () =>
            {
                CleanUpStructuresWorker();
                await CalcStructures(null);
            }).Start();
        }

        private void CleanUpStructuresWorker()
        {
            var Timer = new Stopwatch();
            Timer.Start();

            var MoveToDirectory = Path.Combine(EmpyrionConfiguration.SaveGamePath, Configuration.Current.MoveToDirectory);
            List<string> Errors = null;
            
            Array.ForEach(mPossibleCleanUpObjects, O => {
                try
                {
                    if (Configuration.Current.DeletePermanent) O.Delete();
                    else                                       O.MoveTo(MoveToDirectory);
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

    }
}

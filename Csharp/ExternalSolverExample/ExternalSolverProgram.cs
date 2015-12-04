using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BtmI2p.ComputableTaskInterfaces.Client;
using BtmI2p.MiscUtils;
using BtmI2p.TaskSolvers.Scrypt;
using CommandLine;
using Nito.AsyncEx;
using NLog;

namespace ExternalSolverExample
{
    public class ExternalSolverOptions
    {
        [Option(
            "td", 
            Required = true, 
            HelpText =
                "Problem description (base64 of utf8 string " +
                "of serialized <ComputableTaskSerializedDescription>"
        )]
        public string TaskDescription { get; set; }
        /**/
        [Option(
            "outdir",
            Required = true,
            HelpText =
                "Directory path to save problem solutions"
        )]
        public string OutDirectoryName { get; set; }
        /**/
        [Option(
            "thread-count",
            Required = false,
            DefaultValue = 2
        )]
        public int ThreadCount { get; set; }
        /* 0-managed, 1-windll, 2-linuxso*/
        [Option(
            "native-support",
            Required = false,
            DefaultValue = 0,
            HelpText = "0-managed, 1-windll, 2-linuxso"
        )]
        public int NativeSupport { get; set; }
    }
    public class ExternalSolverProgram
    {
        private static readonly CancellationTokenSource _cts
            = new CancellationTokenSource();
        private static readonly Logger _logger
            = LogManager.GetCurrentClassLogger();

        private static async Task MainAsync(string[] args)
        {
            try
            {
                CultureInfo.DefaultThreadCurrentUICulture
                    = CultureInfo.InvariantCulture;
            }
            catch (NotImplementedException)
            {
            }
            /**/
            try
            {
                var options = new ExternalSolverOptions();
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    if (!Directory.Exists(options.OutDirectoryName))
                    {
                        _logger.Error("Out directory not exists");
                        return;
                    }
                    ComputableTaskSerializedDescription taskSerializedDesc;
                    try
                    {
                        taskSerializedDesc
                            = Encoding.UTF8.GetString(
                                Convert.FromBase64String(options.TaskDescription)
                            ).ParseJsonToType<ComputableTaskSerializedDescription>();
                    }
                    catch (Exception exc)
                    {
                        _logger.Error(
                            "Parse task description error: '{0}'",
                            exc.Message
                        );
                        return;
                    }
                    var solutionFilePath = Path.Combine(
                        options.OutDirectoryName,
                        string.Format(
                            "{0}.json", 
                            taskSerializedDesc.CommonInfo.TaskGuid
                        )
                    );
                    if (File.Exists(solutionFilePath))
                    {
                        _logger.Error(
                            "Task solution already exists"
                        );
                        return;
                    }
                    
                    if (
                        taskSerializedDesc.CommonInfo.TaskType
                        == (int) ETaskTypes.Scrypt
                        )
                    {
                        var taskDesc =
                            taskSerializedDesc.TaskDescriptionSerialized
                                .ParseJsonToType<ScryptTaskDescription>();
                        var maxThreads = options.ThreadCount;
                        _logger.Trace("{0}",new
                        {
                            taskDesc,
                            taskSerializedDesc.CommonInfo,
                            maxThreads
                        }.WriteObjectToJson());
                        var taskSolution
                            = await ScryptTaskSolver.SolveScryptTask(
                                new ComputableTaskDescription<
                                    ScryptTaskDescription
                                    >()
                                {
                                    TaskDescription = taskDesc,
                                    CommonInfo =
                                        taskSerializedDesc.CommonInfo
                                },
                                _cts.Token,
                                maxThreads,
                                options.NativeSupport == 0 
                                    ? ScryptTaskSolver.EUseNativeScrypt.None 
                                    : options.NativeSupport == 1
                                        ? ScryptTaskSolver.EUseNativeScrypt.WinDll 
                                        : ScryptTaskSolver.EUseNativeScrypt.LinuxSo
                            ).ConfigureAwait(false);
                        var result = new ComputableTaskSerializedSolution()
                        {
                            CommonInfo = taskSerializedDesc.CommonInfo,
                            TaskSolutionSerialized
                                = taskSolution.TaskSolution.WriteObjectToJson()
                        };
                        File.WriteAllText(
                            solutionFilePath,
                            result.WriteObjectToJson(),
                            Encoding.UTF8
                        );
                    }
                    else
                    {
                        _logger.Error(
                            "Wrong task type"
                        );
                        return;
                    }
                }
                else
                {
                    _logger.Error("Parse command line args error");
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _cts.Cancel();
            }
        }

        public static void Main(string[] args)
        {
            Task.Factory.StartNew(
                () => {
                    while (true)
                    {
                        var str = Console.In.ReadLine();
                        if (str == "cancel")
                        {
                            _cts.Cancel();
                            return;
                        }
                    }
                },
                TaskCreationOptions.LongRunning 
                    | TaskCreationOptions.DenyChildAttach
            );
            AsyncContext.Run(() => MainAsync(args));
        }
    }
}

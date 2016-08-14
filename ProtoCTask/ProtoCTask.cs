using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;

namespace ProtoCTask
{
    public class ProtoC : ToolTask
    {
        public ProtoC()
        {
            _outputItems = new List<ITaskItem>();
            _inputFileNames = new List<string>();
            _inputDirectories = new HashSet<string>();
        }

        public ITaskItem[] Include { get; set; }

        [Output]
        public ITaskItem[] Outputs
        {
            get
            {
                return _outputItems.ToArray();
            }
        }

        public string TrackerLogLocation { get; set; }

        private List<ITaskItem> _outputItems;

        public string IntermediateOutputPath { get; set; }

        public string ProtoCPath { get; set; }

        private ISet<string> _inputDirectories;
        private List<string> _inputFileNames;

        protected override string ToolName
        {
            get
            {
                return "protoc.exe";
            }
        }

        protected override string GenerateFullPathToTool()
        {
            return Path.Combine(ProtoCPath, ToolName);
        }

        ITaskItem _writeTLog(string name, Dictionary<string, List<string>> dependencies)
        {
            if (string.IsNullOrEmpty(TrackerLogLocation))
            {
                return null;
            }
            string path = Path.Combine(TrackerLogLocation, String.Format("ProtoC.{0}.1.tlog", name));
            using (StreamWriter outFile = new StreamWriter(path, false, Encoding.UTF8))
            {
                foreach (KeyValuePair<string, List<string>> entry in dependencies)
                {
                    outFile.WriteLine("^" + entry.Key);
                    if (entry.Value != null)
                    {
                        foreach (string dep in entry.Value)
                        {
                            outFile.WriteLine(dep);
                        }
                    }

                }
            }
            return new TaskItem(path);
        }

        public override bool Execute()
        {
            System.IO.Directory.CreateDirectory(IntermediateOutputPath);

            Dictionary<string, List<string>> protobufOutDepGraph = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> protoFileInDepGraph = new Dictionary<string, List<string>>();

            Array.ForEach<ITaskItem>(Include, new Action<ITaskItem>(item =>
            {
                string path = item.GetMetadata("FullPath");
                string absolutePathContainingInputFile = Path.GetDirectoryName(path);

                _inputFileNames.Add(path);
                _inputDirectories.Add(absolutePathContainingInputFile);

                string pbName = Path.GetFileName(Path.ChangeExtension(item.ItemSpec, ".pb.cc"));
                string outputItemName = Path.Combine(IntermediateOutputPath, pbName);
                TaskItem outputItem = new TaskItem(outputItemName);
                _outputItems.Add(outputItem);

                protobufOutDepGraph.Add(path, new List<string> { outputItem.GetMetadata("FullPath") });
                protoFileInDepGraph.Add(path, null);
            }));

            // CTIF requires dependency entries for all of its input files that map to output files,
            // even if those dependencies are empty.
            ITaskItem writeTlogItem = _writeTLog("write", protobufOutDepGraph);
            ITaskItem readTlogItem = _writeTLog("read", protoFileInDepGraph);
            CanonicalTrackedOutputFiles trackedOut = new CanonicalTrackedOutputFiles(this, new ITaskItem[] { writeTlogItem });
            CanonicalTrackedInputFiles trackedIn = new CanonicalTrackedInputFiles(this, new ITaskItem[] { readTlogItem }, Include, null, trackedOut, false, false);

            Include = trackedIn.ComputeSourcesNeedingCompilation();

            bool ret = base.Execute();

            trackedOut.SaveTlog();
            trackedIn.SaveTlog();

            return ret;
        }

        protected override bool SkipTaskExecution()
        {
            return Include.Length == 0;
        }

        protected override string GenerateCommandLineCommands()
        {
            List<string> args = new List<string>();
            foreach (string path in _inputDirectories)
            {
                args.Add("-I" + path);
            }

            args.Add("--cpp_out=" + IntermediateOutputPath);
            args.AddRange(_inputFileNames);

            return String.Join(" ", args.ConvertAll(_escapeAndQuote));
        }

        private string _escapeAndQuote(string arg)
        {
            if (arg.IndexOf(' ') == -1)
            {
                return arg;
            }

            if (arg.Last() == '\\')
            {
                arg += '\\';
            }
            return "\"" + arg + "\"";
        }
    }
}

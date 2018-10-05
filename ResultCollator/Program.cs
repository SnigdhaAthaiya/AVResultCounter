using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ResultCollator
{
    class Checker
    {
        private static string withDep = "withDep";
        private static string withoutDep = "withoutDep";
        private static string avOut = "stdout.txt";
        private static string favOut = "FAVstdout.txt";
        private static string angelFname = "Angelic";
        private static string traceFname = "Trace";
        private static string cpuTimeStr = "Cpu(s) :";
        private static string corralTimeStr = "run.corral(s) :";
        private static string eeTimeStr = "explain.error(s) :";
        private static string infoFolder = "Info";
        private static string infoFile = "dependencyInfo.txt";
        private static string resFolder = "FAVResults";
        private static string cpuRes = "cpuTime.txt";
        private static string corralRes = "corralTime.txt";
        private static string eeRes = "eeTime.txt";
        private static string angelicRes = "bugs.txt";
        private static string blockRes = "blocks.txt";
        private static string assertNumRes = "assertsChecked.txt";
        private static string avnresFile = "avnResults.txt";
        private static string traceLenFile = "traceLengths.txt";
        //total number of angelic failures reported
        private static string failCountStr = "bug.count :";

        //total number of blocks reported
        private static string blockCountStr = "blocked.count :";

        //total number of asserts 
        private static string assertCountStr = ":";

        private static string corralTOStr = "Corral call terminates inconclusively with";
        private static string FAVTOStr = "FastAvn deadline reached; consolidating results";
        private static string corralIterTimeStr = "run.corral.iterative(s) :";

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: <Directory Name>");
                return;
            }

            string parentDir = args[0];

            foreach(var dir in Directory.EnumerateDirectories(parentDir))
            {
                string benchName = extractEpNameFromDir(dir);
                Console.WriteLine("\nBenchmark : {0}", benchName);
                
                HashSet<string> entrypoints = getEntrypoints(Path.Combine(Path.Combine(dir, withoutDep), infoFolder, infoFile));
                Console.WriteLine("Total Number of entrypoints : {0}", entrypoints.Count);
                Console.WriteLine("Reporting time for non-time out entrypoints...");
                string resultsDir = Path.Combine(dir, resFolder);
                try
                {

                    if (Directory.Exists(resultsDir))
                    {
                        Directory.Delete(resultsDir,true);
                    }
                    Directory.CreateDirectory(resultsDir);


                }
                catch(Exception e)
                {
                    Console.WriteLine("error :" + e.Message);
                }
                printStats(dir, entrypoints, resultsDir);
                

            }


        }


        //this method will extract and finally print all the metrics
        private static void printStats(string benchmarkDir, HashSet<string> entrypoints, string resultsDir)
        {
            string dirWithout = Path.Combine(benchmarkDir, withoutDep);
            string dirWith = Path.Combine(benchmarkDir, withDep);

            //cpu stats
            Dictionary<string, Tuple<Stats, Stats>> finalStats = new Dictionary<string, Tuple<Stats, Stats>>();
            double totalCpuTimeWithout = 0.0;
            double totalCpuTimeWith = 0.0;

            //FastAVN timeout 
            bool favTOWithout = false;
            bool favTOWith = false;

            //corral TO
            int epTOWithout = 0;
            int epTOWith = 0;

            //corral time
            double totalCorralTimeWithout = 0;
            double totalCorralTimeWith = 0;

            //ee time
            double totalEETimeWithout = 0;
            double totalEETimeWith = 0;

            //corral iter time
            double totalCorralIterTimeWithout = 0;
            double totalCorralIterTimeWith = 0;

            //total angelic bugs reported
            int totalAngelicBugsWithout = 0;
            int totalAngelicBugsWith = 0;

            //total blocks reported
            int totalBlocksWithout = 0;
            int totalBlocksWith = 0;

            //assertions checked in total
            int totalAssertsWithout = 0;
            int totalAssertsWith = 0;

            //cumulative length of the traces - means the stack length
            int totalALWithout = 0;
            int totalALWith = 0;



            string favOutFileWithout = Path.Combine(dirWithout, favOut);
            string favOutFileWith = Path.Combine(dirWith, favOut);
            List<string> epWithSkipped = new List<string>();


            favTOWithout = checkFavTO(favOutFileWithout);
            favTOWith = checkFavTO(favOutFileWith);
            epWithSkipped = getSkippedEP(favOutFileWith);

            List<string> epFAVTOWithout = new List<string>();
            List<string> epFAVTOWith = new List<string>();
           
            foreach (var ep in entrypoints)
            {
                try
                {
                    string epDirWithout = Path.Combine(dirWithout, ep);
                    string epDirWith = Path.Combine(dirWith, ep);
                    
                    //getting stdout.txt
                    string avOutFileWithout = Path.Combine(epDirWithout, avOut);
                    string avOutFileWith = Path.Combine(epDirWith, avOut);

                   //getting avanResults.txt
                    string avnresFileWithout = Path.Combine(epDirWithout, avnresFile);
                    string avnresFileWith = Path.Combine(epDirWith, avnresFile);

                    //getting the traceLengths.txt file
                    string traceLengthFileWithout = Path.Combine(epDirWithout, traceLenFile);
                    string traceLengthFileWith = Path.Combine(epDirWith, traceLenFile);


                    Stats statsWithout = new Stats(ep);
                    Stats statsWith = new Stats(ep);

                    if (!Directory.Exists(epDirWithout))
                    {
                        epFAVTOWithout.Add(ep);
                    }

                    if(!Directory.Exists(epDirWith) && !epWithSkipped.Contains(ep))
                    {
                        epFAVTOWith.Add(ep);
                    }

                    if (File.Exists(avOutFileWithout))
                    {
                        statsWithout = getStdOutMetrics(statsWithout, avOutFileWithout);
                       
                    }

                    if(File.Exists(avOutFileWith))
                    {
                        statsWith = getStdOutMetrics(statsWith, avOutFileWith);
                    }

                    if(File.Exists(avnresFileWithout))
                    {
                        statsWithout.Asserts = getAssertsChecked(avnresFileWithout);
                        totalAssertsWithout += statsWithout.Asserts;
                    }

                    if(File.Exists(avnresFileWith))
                    {
                        statsWith.Asserts = getAssertsChecked(avnresFileWith);
                        totalAssertsWith += statsWith.Asserts;
                    }


                    if (statsWithout.CpuTime != -1)
                    {
                        totalCpuTimeWithout += statsWithout.CpuTime;
                        totalCpuTimeWith += statsWith.CpuTime;
                    }

                    if (statsWithout.CorralTime != -1 )
                    {
                        totalCorralTimeWithout += statsWithout.CorralTime;
                        totalCorralTimeWith += statsWith.CorralTime;
                    }

                    if (statsWithout.CorralIterTime != -1 )
                    {
                        totalCorralIterTimeWithout += statsWithout.CorralIterTime;
                        totalCorralIterTimeWith += statsWith.CorralIterTime;
                    }

                    if (statsWithout.EeTime != -1 )
                    {
                        totalEETimeWithout += statsWithout.EeTime;
                        totalEETimeWith += statsWith.EeTime;
                    }

                    if (statsWithout.CorralTO)
                        epTOWithout++;

                    if (statsWith.CorralTO)
                        epTOWith++;

                    totalAngelicBugsWithout += statsWithout.Bugs;
                    totalAngelicBugsWith += statsWith.Bugs;

                    totalBlocksWithout += statsWithout.Blocks;
                    totalBlocksWith += statsWith.Blocks;


                    statsWithout.AngelicLen= getAngelicLength(epDirWithout, traceLengthFileWithout);
                    statsWith.AngelicLen = getAngelicLength(epDirWith, traceLengthFileWith);
                    totalALWithout += statsWithout.AngelicLen;
                    totalALWith += statsWith.AngelicLen;

                    finalStats.Add(ep, new Tuple<Stats, Stats>(statsWithout, statsWith));

                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not handle entrypoint {0}, due to {1}", ep, e.Message);
                }
            }

            if (finalStats.Keys.Count > 0)
            {
                string summaryFile = Path.Combine(resultsDir, "summary.txt");
                string cputfname = Path.Combine(resultsDir, "cpuTime.txt");
                string corralTFname = Path.Combine(resultsDir, "corralTime.txt");
                string corralIterFname = Path.Combine(resultsDir, "corralIterTime.txt");
                string eeTFname = Path.Combine(resultsDir, "eeTime.txt");
                string angBugFname = Path.Combine(resultsDir, "bugsNum.txt");
                string blockfname = Path.Combine(resultsDir, "blocksnum.txt");
                string assertsCheckedFname = Path.Combine(resultsDir, "assertsConsidered.txt");
                string angelicLengthFname = Path.Combine(resultsDir, "traceLength.txt");


                File.AppendAllText(summaryFile, "Summarized results \n");

                Console.WriteLine();
                Console.WriteLine("Did Fast AVN timeout in without dep  :  " + favTOWithout);
                Console.WriteLine("Did fast AVN timeout in with : " + favTOWith);
                File.AppendAllText(summaryFile,"Did Fast AVN timeout in without dep  :  " + favTOWithout + "\n");
                File.AppendAllText(summaryFile, "Entrypoints not covered due to global time out: " + epFAVTOWithout.Count + "\n");
                File.AppendAllLines(summaryFile, epFAVTOWithout);
                File.AppendAllText(summaryFile,"\n");
                File.AppendAllText(summaryFile, "Did fast AVN timeout in with : " + favTOWith + "\n");
                File.AppendAllText(summaryFile, "Entrypoints not covered due to global time out in with:"+ epFAVTOWith.Count + "\n");
                File.AppendAllLines(summaryFile, epFAVTOWith);
                File.AppendAllText(summaryFile, "\nentrypoints skipped in with dep, as part of optimization : " + epWithSkipped.Count + "\n");
                File.AppendAllLines(summaryFile, epWithSkipped);


                Console.WriteLine();
                Console.WriteLine("Number of entry points timing out due to corral in without :  {0}", epTOWithout);
                Console.WriteLine("Number of entry points timing out due to corral in with :  {0}", epTOWith);
                File.AppendAllText(summaryFile, "Number of entry points timing out due to corral in without : " + epTOWithout + "\n");
                File.AppendAllText(summaryFile, "Number of entry points timing out due to corral in with : "+ epTOWith + "\n");

                Console.WriteLine();
                Console.WriteLine("total CPU time without dependence : = " + totalCpuTimeWithout + "\n");
                Console.WriteLine("total CPU time with dependence : = " + totalCpuTimeWith + "\n");
                File.AppendAllText(summaryFile, "total CPU time without dependence : = " + totalCpuTimeWithout + "\n");
                File.AppendAllText(summaryFile, "total CPU time with dependence : = " + totalCpuTimeWith + "\n");


                Console.WriteLine();
                Console.WriteLine("total Corral time without dependence : = " + totalCorralTimeWithout);
                Console.WriteLine("total Corral time with dependence : = " + totalCorralTimeWith);
                File.AppendAllText(summaryFile, "total Corral time without dependence : = " + totalCorralTimeWithout + "\n");
                File.AppendAllText(summaryFile, "total Corral time with dependence : = " + totalCorralTimeWith + "\n");

                Console.WriteLine();
                Console.WriteLine("total Corral Iter time without dependence : = " + totalCorralIterTimeWithout);
                Console.WriteLine("total Corral Iter time with dependence : = " + totalCorralIterTimeWith);
                File.AppendAllText(summaryFile, "total Corral Iter time without dependence : = " + totalCorralIterTimeWithout + "\n");
                File.AppendAllText(summaryFile, "total Corral Iter time with dependence : = " + totalCorralIterTimeWith + "\n");

                Console.WriteLine();
                Console.WriteLine("total EE time without dependence : = " + totalEETimeWithout);
                Console.WriteLine("total EE time with dependence : = " + totalEETimeWith);
                File.AppendAllText(summaryFile, "total EE time without dependence : = " + totalEETimeWithout + "\n");
                File.AppendAllText(summaryFile, "total EE time with dependence : = " + totalEETimeWith + "\n");

                Console.WriteLine();
                Console.WriteLine("total Angelic bugs  without dependence : = " + totalAngelicBugsWithout);
                Console.WriteLine("total angelic bugs with dependence : = " + totalAngelicBugsWith);
                File.AppendAllText(summaryFile, "total Angelic bugs  without dependence : = " + totalAngelicBugsWithout + "\n");
                File.AppendAllText(summaryFile, "total angelic bugs with dependence : = " + totalAngelicBugsWith + "\n");

                Console.WriteLine();
                Console.WriteLine("total Blocks without dependence : = " + totalBlocksWithout);
                Console.WriteLine("total Blocks with dependence : = " + totalBlocksWith);
                File.AppendAllText(summaryFile, "total Blocks without dependence : = " + totalBlocksWithout + "\n");
                File.AppendAllText(summaryFile, "total Blocks with dependence : = " + totalBlocksWith + "\n");

                Console.WriteLine();
                Console.WriteLine("total asserts considered without dependence : = " + totalAssertsWithout);
                Console.WriteLine("total asserts considered with dependence : = " + totalAssertsWith);
                File.AppendAllText(summaryFile, "total asserts considered without dependence : = " + totalAssertsWithout + "\n");
                File.AppendAllText(summaryFile, "total asserts considered with dependence : = " + totalAssertsWith + "\n");

                Console.WriteLine();
                Console.WriteLine("total trace length without dependence : = " + totalALWithout);
                Console.WriteLine("total trace length with dependence : = " + totalALWith);
                File.AppendAllText(summaryFile, "total trace length without dependence : = " + totalALWithout + "\n");
                File.AppendAllText(summaryFile, "total trace length with dependence : = " + totalALWith + "\n");


                foreach (var entry in finalStats)
                {
                    File.AppendAllText(cputfname, entry.Key + " : " + entry.Value.Item1.CpuTime + "\t || \t" + entry.Value.Item2.CpuTime +"\n");
                    File.AppendAllText(corralTFname, entry.Key + " : " + entry.Value.Item1.CorralTime + "\t || \t" + entry.Value.Item2.CorralTime + "\n");
                    File.AppendAllText(corralIterFname, entry.Key + " : " + entry.Value.Item1.CorralIterTime + "\t || \t" + entry.Value.Item2.CorralIterTime + "\n");
                    File.AppendAllText(eeTFname, entry.Key + " : " + entry.Value.Item1.EeTime + "\t || \t" + entry.Value.Item2.EeTime + "\n");
                    File.AppendAllText(angBugFname, entry.Key + " : " + entry.Value.Item1.Bugs + "\t || \t" + entry.Value.Item2.Bugs + "\n");
                    File.AppendAllText(blockfname, entry.Key + " : " + entry.Value.Item1.Blocks + "\t || \t" + entry.Value.Item2.Blocks + "\n");
                    File.AppendAllText(assertsCheckedFname, entry.Key + " : " + entry.Value.Item1.Asserts + "\t || \t" + entry.Value.Item2.Asserts + "\n");
                    File.AppendAllText(angelicLengthFname, entry.Key + " : " + entry.Value.Item1.AngelicLen + "\t || \t" + entry.Value.Item2.AngelicLen + "\n");
                }
            }

        }

        private static int getAngelicLength(string epDir, string tFilename)
        {
            int totalLen = 0;
            try
            {
                //extract the lengths
                Dictionary<string, int> tLenghts = new Dictionary<string, int>();
                string[] lines = File.ReadAllLines(tFilename);
                foreach(var line in lines)
                {
                    string key = line.Split(':')[0].Trim();
                    int val = int.Parse(line.Split(':')[1].Trim());
                    tLenghts.Add(key, val);
                    
                }

                foreach (var f in Directory.EnumerateFiles(epDir))
                {
                    
                    if (f.Contains(traceFname))
                    {
                        
                        string key1 = f.Substring(f.IndexOf(traceFname) + traceFname.Length);
                        string key = key1.Substring(0, key1.IndexOf("."));
                        int keyVal = -1;
                        if(int.TryParse(key, out keyVal))
                        {
                            totalLen += keyVal;
                        }
                            
                    }
                }

            }catch(Exception e)
            {
                Console.WriteLine("error in getAngelicLength :" + e.Message + "\n" + e.StackTrace);
            }


            Console.WriteLine("totalLen = " + totalLen);
            return totalLen;
        }

        private static List<string> getSkippedEP(string favOutfile)
        {
            List<string> skipped = new List<string>();
            try
            {
                string[] lines = File.ReadAllLines(favOutfile);
                foreach(var line in lines)
                {
                    if(line.Contains("[Snigdha] skipping execution of :"))
                    {
                        string ep = line.Split(':')[1].Trim();
                        skipped.Add(ep);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Having problem {0} with file {1}", e.Message, favOutfile);
            }
            return skipped;

        }

        private static int getAssertsChecked(string avnresFile)
        {
            int result = 0;
            try
            {
                string[] lines = File.ReadAllLines(avnresFile);
                result = lines.Length;
            }
            catch (Exception e)
            {
                Console.WriteLine("Having problem {0} with file {1}", e.Message, avnresFile);

            }

            return result;

        }

        private static bool checkFavTO(string favOutFile)
        {
            bool result = false;

            try
            {
                string[] lines = File.ReadAllLines(favOutFile);
                foreach(var line in lines)
                {
                    if(line.Contains(FAVTOStr))
                    {
                        result = true;
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Having problems with file : {0}, {1}", favOutFile, e.Message);
            }

            return result;
        }

        private static Stats getStdOutMetrics(Stats statOb, string avOutFile)
        {
            try
            {

                string[] avOutLines = File.ReadAllLines(avOutFile);

                foreach (var line in avOutLines)
                {
                    if (line.Contains(cpuTimeStr))
                    {
                        int index = line.IndexOf(cpuTimeStr) + cpuTimeStr.Length;
                        string value = line.Substring(index).Trim();
                        statOb.CpuTime = double.Parse(value);
                        continue;
                    }
                    if(line.Contains(corralTimeStr))
                    {
                        int index = line.IndexOf(corralTimeStr) + corralTimeStr.Length;
                        string value = line.Substring(index).Trim();
                        statOb.CorralTime = double.Parse(value);
                        continue;
                    }
                    if (line.Contains(corralIterTimeStr))
                    {
                        int index = line.IndexOf(corralIterTimeStr) + corralIterTimeStr.Length;
                        string value = line.Substring(index).Trim();
                        statOb.CorralIterTime = double.Parse(value);
                        continue;
                    }
                    if (line.Contains(eeTimeStr))
                    {
                        int index = line.IndexOf(eeTimeStr) + eeTimeStr.Length;
                        string value = line.Substring(index).Trim();
                        statOb.EeTime = double.Parse(value);
                        continue;
                    }

                    if (line.Contains(failCountStr))
                    {
                        int index = line.IndexOf(failCountStr) + failCountStr.Length; 
                        string value = line.Substring(index).Trim();
                        statOb.Bugs = int.Parse(value);
                        continue;
                    }
                    if (line.Contains(blockCountStr))
                    {
                        int index = line.IndexOf(blockCountStr) + blockCountStr.Length;
                        string value = line.Substring(index).Trim();
                        statOb.Blocks = int.Parse(value);
                        continue;
                    }
                    if (line.Contains(corralTOStr))
                    {
                        statOb.CorralTO = true;
                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine("trouble with file {0} : {1}", avOutFile, e.Message);

            }

            return statOb;
        }

        private static double getCPUTime(string avOutFileName)
        {
            double time = -1;
            try
            {

                string[] avOutLines = File.ReadAllLines(avOutFileName);

                foreach (var line in avOutLines)
                {
                    if (line.Contains(cpuTimeStr))
                    {
                        int index = line.IndexOf(cpuTimeStr) + cpuTimeStr.Length;
                        string value = line.Substring(index).Trim();
                        time = double.Parse(value);
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("trouble with file {0} : {1}", avOutFileName, e.Message);

            }

            return time;
        }

        private static HashSet<string> getEntrypoints(string depFilePath)
        {
            HashSet<string> ep = new HashSet<string>();
            try
            {
                string[] lines = File.ReadAllLines(depFilePath);
                foreach(var line in lines)
                {
                    if(line.StartsWith("Entrypoint "))
                    {
                        string epname = line.Split(' ')[1];
                        ep.Add(epname);
                        
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error : Entrypoint file has some problems :" + e.Message);
            }
            return ep;
        }

        private static string extractEpNameFromDir(string dir)
        {
            string[] dirparts = dir.Split(Path.DirectorySeparatorChar);
            return dirparts[dirparts.Length - 1];

        }

        private static string getTime(string newFpath)
        {
            string[] lines = File.ReadAllLines(newFpath);
            foreach (var line in lines)
            {
                if (line.Contains("[TAG: AV_STATS] TotalTime(ms) :"))
                {
                    string[] lineParts = line.Split(':');
                    return lineParts[lineParts.Length - 1].Trim();
                }
            }
            return "-1";
        }
    }

    class Stats
    {
        string epName = "";
        double cpuTime = -1;
        double corralTime = -1;
        double eeTime = -1;
        double corralIterTime = -1;
        int bugs = 0;
        int blocks = 0;
        int asserts = 0;
        int angelicLen = 0;
        bool corralTO = false;
        

        public Stats(string ep)
        {
            epName = ep;
        }


        public string EpName { get => epName; set => epName = value; }
        public double CpuTime { get => cpuTime; set => cpuTime = value; }
        public double CorralTime { get => corralTime; set => corralTime = value; }
        public double EeTime { get => eeTime; set => eeTime = value; }
        public double CorralIterTime { get => corralIterTime; set => corralIterTime = value; }
        public int Bugs { get => bugs; set => bugs = value; }
        public int Blocks { get => blocks; set => blocks = value; }
        public int Asserts { get => asserts; set => asserts = value; }
        public int AngelicLen { get => angelicLen; set => angelicLen = value; }
        public bool CorralTO { get => corralTO; set => corralTO = value; }
    }

}

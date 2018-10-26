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
        private static string favCpuTimeStr = "fastavn(s) :";
        private static string corralTimeStr = "run.corral(s) :";
        private static string eeTimeStr = "explain.error(s) :";
        private static string infoFolder = "Info";
        private static string infoFile = "dependencyInfo.txt";
        private static string resFolder = "FAVResults";
        private static string procInlineStr = "Number of procedures inlined: ";
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
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: <Source Directory Name> <Dir1> <Dir2> <Result Folder Name> ");
                Console.WriteLine("args = {0}", args);
                foreach (var arg in args)
                    Console.WriteLine(arg);
                return;
            }

            string parentDir = args[0];
            withoutDep = args[1];
            withDep = args[2];
            resFolder = args[3];

            Console.WriteLine("Folder 1 : {0}", withoutDep);
            Console.WriteLine("Folder 2 : {0}", withDep);

            foreach (var dir in Directory.EnumerateDirectories(parentDir))
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
            string dir1 = Path.Combine(benchmarkDir, withoutDep);
            string dir2 = Path.Combine(benchmarkDir, withDep);

            //cpu stats
            Dictionary<string, Tuple<Stats, Stats>> finalStats = new Dictionary<string, Tuple<Stats, Stats>>();
            double totalCpuTime1 = 0.0;
            double totalCpuTime2 = 0.0;

            //FastAVN timeout 
            bool favTO1 = false;
            bool favTO2 = false;

            //corral TO
            int epTO1 = 0;
            int epTO2 = 0;

            //corral time
            double totalCorralTime1 = 0;
            double totalCorralTime2 = 0;

            //ee time
            double totalEETime1 = 0;
            double totalEETime2 = 0;

            //corral iter time
            double totalCorralIterTime1 = 0;
            double totalCorralIterTime2 = 0;

            //total angelic bugs reported
            int totalAngelicBugs1 = 0;
            int totalAngelicBugs2 = 0;

            //total blocks reported
            int totalBlocks1 = 0;
            int totalBlocks2 = 0;

            //assertions checked in total
            int totalAsserts1 = 0;
            int totalAsserts2 = 0;

            //procs inlined total
            int totalProcsInlined1 = 0;
            int totalProcsInlined2 = 0;
           


            string favOutFile1 = Path.Combine(dir1, favOut);
            string favOutFile2 = Path.Combine(dir2, favOut);
            List<string> epWithSkipped = new List<string>();


            favTO1 = checkFavTO(favOutFile1);
            favTO2 = checkFavTO(favOutFile2);
            epWithSkipped = getSkippedEP(favOutFile2);

            totalCpuTime1 = getCPUTime(favOutFile1);
            totalCpuTime2 = getCPUTime(favOutFile2);

            List<string> epFAVTO1 = new List<string>();
            List<string> epFAVTO2 = new List<string>();
           
            foreach (var ep in entrypoints)
            {
                try
                {
                    string epDir1 = Path.Combine(dir1, ep);
                    string epDir2 = Path.Combine(dir2, ep);
                    
                    //getting stdout.txt
                    string avOutFile1 = Path.Combine(epDir1, avOut);
                    string avOutFile2 = Path.Combine(epDir2, avOut);

                   //getting avnResults.txt
                    string avnresFile1 = Path.Combine(epDir1, avnresFile);
                    string avnresFile2 = Path.Combine(epDir2, avnresFile);

                    //getting the traceLengths.txt file
                   // string traceLengthFileWithout = Path.Combine(epDirWithout, traceLenFile);
                   // string traceLengthFileWith = Path.Combine(epDirWith, traceLenFile);


                    Stats stats1 = new Stats(ep);
                    Stats stats2 = new Stats(ep);

                    if (!Directory.Exists(epDir1) || !File.Exists(avOutFile1) || !runCompleted(avOutFile1))
                    {
                        epFAVTO1.Add(ep);
                    }

                    // TODO check stdout result instead of checking just directory
                    if(!epWithSkipped.Contains(ep) && (!Directory.Exists(epDir2)  || (!File.Exists(avOutFile2)) || !runCompleted(avOutFile2) ))
                    {
                        epFAVTO2.Add(ep);
                    }

                    if (File.Exists(avOutFile1))
                    {
                        stats1 = getStdOutMetrics(stats1, avOutFile1);
                       
                    }

                    if(File.Exists(avOutFile2))
                    {
                        stats2 = getStdOutMetrics(stats2, avOutFile2);
                    }

                    if(File.Exists(avnresFile1))
                    {
                        stats1.Asserts = getAssertsChecked(avnresFile1);
                        totalAsserts1 += stats1.Asserts;
                    }

                    if(File.Exists(avnresFile2))
                    {
                        stats2.Asserts = getAssertsChecked(avnresFile2);
                        totalAsserts2 += stats2.Asserts;
                    }

                    if (stats1.CorralTime != -1 )
                        totalCorralTime1 += stats1.CorralTime;
                    
                    if (stats2.CorralTime != -1)
                        totalCorralTime2 += stats2.CorralTime;
                    

                    if (stats1.CorralIterTime != -1 )
                        totalCorralIterTime1 += stats1.CorralIterTime;

                    if (stats2.CorralIterTime != -1)
                        totalCorralIterTime2 += stats2.CorralIterTime;
                    

                    if (stats1.EeTime != -1 )
                        totalEETime1 += stats1.EeTime;

                    if (stats2.EeTime != -1)
                        totalEETime2 += stats2.EeTime;
                    

                    if (stats1.CorralTO)
                        epTO1++;

                    if (stats2.CorralTO)
                        epTO2++;

                    totalAngelicBugs1 += stats1.Bugs;
                    totalAngelicBugs2 += stats2.Bugs;

                    totalBlocks1 += stats1.Blocks;
                    totalBlocks2 += stats2.Blocks;

                    totalProcsInlined1 += stats1.ProcsInlined;
                    totalProcsInlined2 += stats2.ProcsInlined;
                   
                    finalStats.Add(ep, new Tuple<Stats, Stats>(stats1, stats2));

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
                string procInlineFname =Path.Combine(resultsDir, "procsInlined.txt");

                File.AppendAllText(summaryFile, "Summarized results \n");

                Console.WriteLine();
                Console.WriteLine("Did Fast AVN timeout in 1  :  " + favTO1);
                Console.WriteLine("Did fast AVN timeout in 2 : " + favTO2);
                File.AppendAllText(summaryFile,"Did Fast AVN timeout in 1  :  " + favTO1 + "\n");
                File.AppendAllText(summaryFile, "Entrypoints not covered due to global time out: " + epFAVTO1.Count + "\n");
                File.AppendAllLines(summaryFile, epFAVTO1);
                File.AppendAllText(summaryFile,"\n");
                File.AppendAllText(summaryFile, "Did fast AVN timeout in 2 : " + favTO2 + "\n");
                File.AppendAllText(summaryFile, "Entrypoints not covered due to global time out in 2:"+ epFAVTO2.Count + "\n");
                File.AppendAllLines(summaryFile, epFAVTO2);
                File.AppendAllText(summaryFile, "\nentrypoints skipped in 2, as part of optimization : " + epWithSkipped.Count + "\n");
                File.AppendAllLines(summaryFile, epWithSkipped);


                Console.WriteLine();
                Console.WriteLine("Number of entry points timing out due to corral in 1 :  {0}", epTO1);
                Console.WriteLine("Number of entry points timing out due to corral in 2 :  {0}", epTO2);
                File.AppendAllText(summaryFile, "Number of entry points timing out due to corral in 1 : " + epTO1 + "\n");
                File.AppendAllText(summaryFile, "Number of entry points timing out due to corral in 2 : "+ epTO2 + "\n");

                Console.WriteLine();
                Console.WriteLine("total CPU time 1 : = " + totalCpuTime1 + "\n");
                Console.WriteLine("total CPU time 2 : = " + totalCpuTime2 + "\n");
                File.AppendAllText(summaryFile, "total CPU time 1 : = " + totalCpuTime1 + "\n");
                File.AppendAllText(summaryFile, "total CPU time 2 : = " + totalCpuTime2 + "\n");


                Console.WriteLine();
                Console.WriteLine("total Corral time 1 : = " + totalCorralTime1);
                Console.WriteLine("total Corral time 2 : = " + totalCorralTime2);
                File.AppendAllText(summaryFile, "total Corral time 1 : = " + totalCorralTime1 + "\n");
                File.AppendAllText(summaryFile, "total Corral time 2 : = " + totalCorralTime2 + "\n");

                Console.WriteLine();
                Console.WriteLine("total Corral Iter time 1 : = " + totalCorralIterTime1);
                Console.WriteLine("total Corral Iter time 2 : = " + totalCorralIterTime2);
                File.AppendAllText(summaryFile, "total Corral Iter time 1 : = " + totalCorralIterTime1 + "\n");
                File.AppendAllText(summaryFile, "total Corral Iter time 2 : = " + totalCorralIterTime2 + "\n");

                Console.WriteLine();
                Console.WriteLine("total EE time 1 : = " + totalEETime1);
                Console.WriteLine("total EE time 2 : = " + totalEETime2);
                File.AppendAllText(summaryFile, "total EE time 1 : = " + totalEETime1 + "\n");
                File.AppendAllText(summaryFile, "total EE time 2 : = " + totalEETime2 + "\n");

                Console.WriteLine();
                Console.WriteLine("total Angelic bugs  1 : = " + totalAngelicBugs1);
                Console.WriteLine("total angelic bugs 2 : = " + totalAngelicBugs2);
                File.AppendAllText(summaryFile, "total Angelic bugs  1 : = " + totalAngelicBugs1 + "\n");
                File.AppendAllText(summaryFile, "total angelic bugs 2 : = " + totalAngelicBugs2 + "\n");

                Console.WriteLine();
                Console.WriteLine("total Blocks 1 : = " + totalBlocks1);
                Console.WriteLine("total Blocks 2 : = " + totalBlocks2);
                File.AppendAllText(summaryFile, "total Blocks 1 : = " + totalBlocks1 + "\n");
                File.AppendAllText(summaryFile, "total Blocks 2 : = " + totalBlocks2 + "\n");

                Console.WriteLine();
                Console.WriteLine("total procedures inlined 1 : = " + totalProcsInlined1);
                Console.WriteLine("total procedures inlined 2 : = " + totalProcsInlined2);
                File.AppendAllText(summaryFile, "total procedures inlined 1 : = " + totalProcsInlined1 + "\n");
                File.AppendAllText(summaryFile, "total procedures inlined 2 : = " + totalProcsInlined2 + "\n");

                Console.WriteLine();
                Console.WriteLine("total asserts checked 1 : = " + totalAsserts1);
                Console.WriteLine("total asserts checked 2 : = " + totalAsserts2);
                File.AppendAllText(summaryFile, "total asserts checked 1 : = " + totalAsserts1 + "\n");
                File.AppendAllText(summaryFile, "total asserts checked 2 : = " + totalAsserts2 + "\n");


                foreach (var entry in finalStats)
                {
                    File.AppendAllText(cputfname, entry.Key + " : " + entry.Value.Item1.CpuTime + "\t || \t" + entry.Value.Item2.CpuTime +"\n");
                    File.AppendAllText(corralTFname, entry.Key + " : " + entry.Value.Item1.CorralTime + "\t || \t" + entry.Value.Item2.CorralTime + "\n");
                    File.AppendAllText(corralIterFname, entry.Key + " : " + entry.Value.Item1.CorralIterTime + "\t || \t" + entry.Value.Item2.CorralIterTime + "\n");
                    File.AppendAllText(eeTFname, entry.Key + " : " + entry.Value.Item1.EeTime + "\t || \t" + entry.Value.Item2.EeTime + "\n");
                    File.AppendAllText(angBugFname, entry.Key + " : " + entry.Value.Item1.Bugs + "\t || \t" + entry.Value.Item2.Bugs + "\n");
                    File.AppendAllText(blockfname, entry.Key + " : " + entry.Value.Item1.Blocks + "\t || \t" + entry.Value.Item2.Blocks + "\n");
                    File.AppendAllText(assertsCheckedFname, entry.Key + " : " + entry.Value.Item1.Asserts + "\t || \t" + entry.Value.Item2.Asserts + "\n");
                    File.AppendAllText(procInlineFname, entry.Key + " : " + entry.Value.Item1.ProcsInlined + "\t || \t" + entry.Value.Item2.ProcsInlined + "\n");
                    //  File.AppendAllText(angelicLengthFname, entry.Key + " : " + entry.Value.Item1.AngelicLen + "\t || \t" + entry.Value.Item2.AngelicLen + "\n");
                }
            }

        }

        private static bool runCompleted(string avOutFileName)
        {
            bool result = false;
            string lastLine = "[TAG: AV_STATS] TotalTime(ms) :";
            try
            {
                string[] lines = File.ReadAllLines(avOutFileName);
                for(int i = lines.Length - 1 ; i >=0; i--)
                {
                    if(lines[i].Contains(lastLine))
                    {
                        result = true;
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Error in runCompleted : {0}", e.Message);
            }
            return result;
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
                        continue;
                    }
                    if (line.Contains(procInlineStr))
                    {
                        int index = line.IndexOf(procInlineStr) + procInlineStr.Length;
                        string value = line.Substring(index).Trim();
                        statOb.ProcsInlined += int.Parse(value);
                        
                    }

                }

            }
            catch (Exception e)
            {
                Console.WriteLine("trouble with file {0} : {1}", avOutFile, e.Message);

            }

            return statOb;
        }

        private static double getCPUTime(string outFileName)
        {
            double time = -1;
            try
            {

                string[] lines = File.ReadAllLines(outFileName);

                for(int i = lines.Length -1; i>=0; i--)
                {
                    string line = lines[i];
                    if (line.Contains(favCpuTimeStr))
                    {
                        int index = line.IndexOf(favCpuTimeStr) + favCpuTimeStr.Length;
                        string value = line.Substring(index).Trim();
                        time = double.Parse(value);
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("trouble with file {0} : {1}", outFileName, e.Message);

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
        int procsInlined = 0;
        

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
        public int ProcsInlined { get => procsInlined; set => procsInlined = value; }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace AVResultCounter
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 3)
            {
                Console.WriteLine("Usage: <Directory Name 1> <Directory Name 2> <File Name>");
                return;
            }
            string dir1 = args[0];
            string dir2 = args[1];
            string filename = args[2];
            Dictionary<string, Tuple<string, double>> time = new Dictionary<string, Tuple<string, double>>();
            //this will take a parent directory name and a file name and search for the file recursively
            if(Directory.Exists(dir1) && Directory.Exists(dir2))
            {
                foreach(var dirres  in Directory.EnumerateDirectories(dir1))
                {
                    string ep = extractEpNameFromDir(dirres);
                    string fn1 = Path.Combine(dirres, filename);
                    string fn2 = Path.Combine(Path.Combine(dir2, ep), filename);
                    string res = "[";
                    double time1 = -1;
                    double time2 = -1;
                    if (File.Exists(fn1))
                    {
                        string t1 = getTime(fn1);
                        time1 = Double.Parse(t1);
                        res = res + t1;
                    }
                    if(File.Exists(fn2))
                    {
                        string t2 = getTime(fn2);
                        time2 = Double.Parse(t2);

                        res = res + ", " + t2;
                    }
                    res = res + "]";
                    double ratio = -1;
                    if (time1 != -1 && time2 != -2)
                        ratio = time2 / time1;
                    time.Add(ep, new Tuple<string, double>(res, ratio));
                }                
            }
            else
            {
                Console.WriteLine("The directory does not exist");
            }

            string outputFile = "timetaken.txt";
            if(File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            File.AppendAllText(outputFile, "[Format] entrypoint --> [nonDep, Dep] \n");
            foreach (var ep in time.Keys)
            {
                File.AppendAllText(outputFile, ep + "-->" + time[ep] + "\n");
            }
            
        }

        private static string extractEpNameFromDir(string dir)
        {
            string[] dirparts = dir.Split(Path.DirectorySeparatorChar);
            return dirparts[dirparts.Length - 1];

        }

        private static string getTime(string newFpath)
        {
            string[] lines = File.ReadAllLines(newFpath);
            foreach(var line in lines)
            {
                if(line.Contains("[TAG: AV_STATS] TotalTime(ms) :"))
                {
                    string[] lineParts = line.Split(':');
                    return lineParts[lineParts.Length - 1].Trim();
                }
            }
            return "-1";
        }
    }
}

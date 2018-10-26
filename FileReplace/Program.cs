using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileReplace
{
    class Program
    {
        static void Main(string[] args)
        {

            if(args.Length<2)
            {
                Console.WriteLine("usage : <tochange.bpl> <fromChange.bpl>");
            }

            string tochange = args[0];
            string fromChange = args[1];
            int linesChanged = 0;
            try
            {
                string[] tochangeLines = File.ReadAllLines(tochange);
                string[] fromChangeLines = File.ReadAllLines(fromChange);
                int tIndex = 0;
                int findex = 0;
                while(tIndex<tochangeLines.Length)
                {
                    string tline = tochangeLines[tIndex];
                    //check and loop
                    if(tline.Trim().StartsWith("assert"))
                    {
                        int len = 7;
                        string targetfline = tline.Replace("assert ", "assume ");

                        for (int i = findex; i< fromChangeLines.Length; i++)
                        {
                            if(fromChangeLines[i].Equals(targetfline))
                            {
                                Console.WriteLine("changing {0} to {1}", tochangeLines[tIndex], targetfline);
                                tochangeLines[tIndex] = targetfline;
                                findex = i;
                                linesChanged++;
                                break;
                            }
                        }
                    }

                    tIndex++;
                }

                string copy = "copy.bpl";
                File.WriteAllLines(copy, tochangeLines);
                Console.WriteLine("lines modified = {0}", linesChanged);

            }
            catch(Exception e)
            {
                Console.WriteLine("error reading files :" + e.Message);
            }


        }
    }
}

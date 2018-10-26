using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LineCompare
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length < 2)
            {
                Console.WriteLine("usage : <file1> <file2>");
            }

            string orig = args[0];
            string newf = args[1];
            HashSet<string> varsNotPresentInOrig = new HashSet<string>();
            HashSet<string> varsNotpresentInNew = new HashSet<string>();

            int linesChanged = 0;
            try
            {
                string[] origLines = File.ReadAllLines(orig);
                string[] newLines = File.ReadAllLines(newf);
                
                for(int i = 0 ; i < origLines.Length; i++)
                {
                    bool flag = false;
                    for(int j=0; j< newLines.Length;j++)
                    {
                        if(origLines[i].Trim().Equals(newLines[j].Trim()))
                        {
                            flag = true;
                        }

                    }
                    if(!flag)
                    {
                        varsNotpresentInNew.Add(origLines[i].Trim());
                    }

                }

                for (int i = 0; i < newLines.Length; i++)
                {
                    bool flag = false;
                    for (int j = 0; j < origLines.Length; j++)
                    {
                        if (newLines[i].Trim().Equals(origLines[j].Trim()))
                        {
                            flag = true;
                        }

                    }
                    if (!flag)
                    {
                        varsNotpresentInNew.Add(newLines[i].Trim());
                    }

                }




            }
            catch (Exception e)
            {
                Console.WriteLine("error reading files :" + e.Message);
            }


            Console.WriteLine("Not present in orig :");
            foreach (var v in varsNotPresentInOrig)
                Console.WriteLine(v);


            Console.WriteLine("Not present in new :");
            foreach (var v in varsNotpresentInNew)
                Console.WriteLine(v);


        }
    }
}

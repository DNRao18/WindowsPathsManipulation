using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsPathsManipulation
{
    class Program
    {
        static void Main(string[] args)
        {
            //Test1
            bool isFileExistedAndNotEmpty = PathUtils.TextFileExistsAndNotEmpty(@"D:\Test.txt");
            bool isPathValid = PathUtils.IsPathValid(@"D:\test.xml");

        }
    }
}

using System;
using System.IO;
using Signature.Properties;

namespace Signature
{
  internal static class Program
  {
    public static void Main(string[] args)
    {
        try
        {
            string outFile = args.Length == 3 ? args[2] : String.Empty;
            SignatureMaker sm = new SignatureMaker(args[0], Int32.Parse(args[1]), 10, outFile);
            sm.Generate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
        }
    }
  }
}
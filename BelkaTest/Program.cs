using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace BelkaTest
{
  class Program
  {
    public static Logger logger = LogManager.GetCurrentClassLogger();
    private const int blockLength = 1024;
    private static Object sync = new Object();
    static void Main(string[] args)
    {
      {
        if (args.Length == 1)
        {
          Console.WriteLine(" /c to compress");
          Console.WriteLine(" /d to decompress");
          Console.WriteLine(" /i={filepath} inputfile");
          Console.WriteLine(" /o={filepath} outputfile");
        }
        string infile = "";
        string outfile = "";
        string operation = "";
        
        int procCount = Environment.ProcessorCount;
        var threads = new List<Thread>(procCount);
        if (args.Length > 1)
        {
          if (args.Contains("/c")) operation = "compress";
          if (args.Contains("/d")) operation = "decompress";
          args.ToList().ForEach(x =>
          {
            if (x.Contains("/i="))
            {
              infile = x.Replace("/i=", "");
            }
            if (x.Contains("/o="))
            {
              outfile = x.Replace("/o=", "");
            }
          });
          Console.WriteLine("Choosed operation: " + operation);
          logger.Trace("Choosed operation: " + operation);
          if (infile != "")
          {
            if (outfile != "")
            {
              Console.WriteLine("Source file: " + infile);
              logger.Trace("Source file: " + infile);
              Console.WriteLine("Output file: " + outfile);
              logger.Trace("Output file: " + outfile);
              if (outfile != infile)
              {
                if (operation == "compress")
                {
                  try
                  {
                    using (FileStream fsin = new FileStream(infile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (FileStream fsout = new FileStream(outfile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    using (GZipStream gz = new GZipStream(fsout, CompressionMode.Compress, false))
                    {
                      var blockCount = fsin.Length / blockLength + 1;
                      var range = Math.Ceiling((double) blockCount / procCount);
                      for (var p = 0; p < procCount; p++)
                      {
                        var start = Convert.ToInt32(p * range);
                        var stop = Convert.ToInt32(start + range);

                        threads.Add(new Thread(() =>
                        {
                          using (FileStream fsinput = new FileStream(infile, FileMode.Open, FileAccess.Read,
                            FileShare.Read))
                          {
                            var len = 0;
                            for (var i = start; i < stop; i++)
                            {
                              lock (sync)
                              {
                                var buf = new byte[blockLength];
                                len = fsinput.Read(buf, 0, blockLength);
                                gz.Write(buf, 0, len);
                              }
                              if (len < blockLength) break;
                            }
                          }
                        }));

                      }
                      threads.ForEach(x => x.Start());
                      threads.ForEach(x => x.Join());
                    }
                    Console.WriteLine("Done");
                    logger.Trace("Done");
                    Console.ReadKey();
                    return;
                  }
                  catch (Exception ee)
                  {
                    Handler.ErrorHandler(ee, logger);
                    return;
                  }
                }
                if (operation == "decompress")
                {
                  try
                  {
                    using (FileStream fsin = new FileStream(infile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (FileStream fsout = new FileStream(outfile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    using (GZipStream gz = new GZipStream(fsin, CompressionMode.Decompress, false))
                    {
                      
                      for (var p = 0; p < procCount; p++)
                      {
                        threads.Add(new Thread(() =>
                        {
                          try
                          {
                            var len = 0;
                            do
                            {
                              lock (sync)
                              {
                                var buf = new byte[blockLength];
                                len = gz.Read(buf, 0, blockLength);
                                fsout.Write(buf, 0, len);
                              }
                            } while (len >= 1024);
                          }
                          catch (Exception ee)
                          {
                            Handler.ErrorHandler(ee, logger);
                          }
                        }));

                      }
                      threads.ForEach(x => x.Start());
                      threads.ForEach(x => x.Join());
                    }
                    Console.WriteLine("Done");
                    logger.Trace("Done");
                    Console.ReadKey();
                  }
                  catch (Exception ee)
                  {
                    Handler.ErrorHandler(ee, logger);
                  }
                }
              }
              else
              {
                Handler.WarningHandler("Input and Output files have the same names", logger);
              }
            }
            else
            {
              Handler.WarningHandler("Output file name not specified!", logger);
            }
          }
          else
          {
            Handler.WarningHandler("Input file name not specified", logger);
          }
        }
      }
    }

  }

  static class Handler
  {
    public static void ErrorHandler(Exception e, Logger log)
    {
      Console.WriteLine("ERROR\n\r" + e.Message + "\n\r" + e.Source + "\n\r" + e.StackTrace);
      log.Error(e.Message + "\n\r" + e.Source + "\n\r" + e.StackTrace);
    }

    public static void WarningHandler(String message, Logger log)
    {
      Console.WriteLine("WARNING\n\r" + message);
      log.Warn(message);
    }
  }
}

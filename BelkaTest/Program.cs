using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BelkaTest
{
  class Program
  {
    static void Main(string[] args)
    {
      {
        if (args.Length == 1)
        {
          Console.WriteLine(" /u to unzip");
          Console.WriteLine(" /z to zip");
          Console.WriteLine(" /i={filepath} inputfile");
          Console.WriteLine(" /o={filepath} outputfile");
        }
        string infile = "";
        string outfile = "";
        string operation = "";
        var blockLength = 1024;
        var bufferDictionary = new ConcurrentDictionary<int, byte[]>();
        int procCount = Environment.ProcessorCount;
        var threads = new List<Thread>(procCount);
        if (args.Length > 1)
        {
          if (args.Contains("/z")) operation = "zip";
          if (args.Contains("/u")) operation = "unzip";
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
          if (infile != "")
          {
            if (outfile != "")
            {
              Console.WriteLine("Source file: " + infile);
              Console.WriteLine("Output file: " + outfile);
              if (outfile != infile)
              {
                if (operation == "zip")
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
                            for (var i = start; i < stop; i++)
                            {
                              var buf = new byte[blockLength];
                              var len = fsinput.Read(buf, 0, blockLength);
                              gz.Write(buf, 0, len);
                              if (len < 1024) break;
                              //bufferDictionary[i] = buf;
                            }
                          }
                        }));

                      }
                      threads.ForEach(x => x.Start());
                      threads.ForEach(x => x.Join());

                     // do
                     // {
                     //   len = fsin.Read(buffer, 0, 1024);
                     //   gz.Write(buffer, 0, len);
                     //   pos += len;
                     // } while (len >= 1024);
                    }
                    Console.WriteLine("OK");
                    Console.ReadKey();
                    return;
                  }
                  catch (Exception ee)
                  {
                    Console.WriteLine("ERROR\n\r" + ee.Message + "\n\r" + ee.Source + "\n\r" + ee.StackTrace);
                    return;
                  }
                }
                if (operation == "unzip")
                {
                  try
                  {
                    using (FileStream fsin = new FileStream(infile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (FileStream fsout = new FileStream(outfile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    using (GZipStream gz = new GZipStream(fsin, CompressionMode.Decompress, false))
                    {
                      byte[] buffer = new byte[1024];
                      int pos = 0;
                      int len;
                      do
                      {
                        len = gz.Read(buffer, 0, 1024);
                        fsout.Write(buffer, 0, len);
                        pos += len;
                      } while (len >= 1024);
                    }
                    Console.WriteLine("OK");
                    Console.ReadKey();
                    return;
                  }
                  catch (Exception ee)
                  {
                    Console.WriteLine("ERROR\n\r" + ee.Message + "\n\r" + ee.Source + "\n\r" + ee.StackTrace);
                    return;
                  }
                }
              }
              else
              {
                Console.WriteLine("ERROR\n\rИмя исходного и конечного файлов совпадают!");
                return;
              }
            }
            else
            {
              Console.WriteLine("ERROR\n\rИмя конечного файла не указано!");
              return;
            }
          }
          else
          {
            Console.WriteLine("ERROR\n\rИмя исходного файла не указано!");
            return;
          }
        }
      }
    }


  }
}

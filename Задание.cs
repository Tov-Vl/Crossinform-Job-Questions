using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace CrossinformTest
{
    class Program
    {
        static CancellationTokenSource s_cts = new CancellationTokenSource();

        static IEnumerable<KeyValuePair<string, int>> TripletsTop;

        static IEnumerable<KeyValuePair<string, int>> MostUsedTriplets(string FileName, int TripletsNum)
        {
            var TripletsDict = new ConcurrentDictionary<string, int>();
            var SplitChars = new char[] { ' ' };

            ParallelOptions options = new ParallelOptions() { CancellationToken = s_cts.Token };

            try
            {
                Parallel.ForEach(File.ReadLines(FileName), options, delegate (string line, ParallelLoopState state, long lineNumber)
                {
                    line = line.ToLower();
                    line = line.Replace("!", "");
                    line = line.Replace(",", "");
                    line = line.Replace(".", "");

                    var Words = line.Split(SplitChars);

                    foreach (var word in Words)
                    {
                        int freq;

                        for (var i = 0; i <= word.Length - 3; i++)
                        {
                            string triplets = word.Substring(i, 3);

                            TripletsDict.TryGetValue(triplets, out freq);
                            freq += 1;

                            TripletsDict.AddOrUpdate(triplets, freq, ((str, value) => freq));
                        }
                    }
                });

                return TripletsDict.ToList().OrderBy(x => -x.Value).Take(TripletsNum);
            }
            catch (OperationCanceledException)
            {
                return TripletsDict.ToList().OrderBy(x => -x.Value).Take(TripletsNum);
            }
            finally
            {
                s_cts.Dispose();
            }
        }

        private static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            string FileName;

            Console.WriteLine("Введите путь к файлу:");
            FileName = Console.ReadLine();
            while (!(File.Exists(FileName)))
            {
                Console.WriteLine("Файл {0} не найден! Введите путь к файлу:", FileName);
                FileName = Console.ReadLine();
            }

            stopwatch.Start();

            var cancelTask = Task.Run(() =>
            {
                Console.WriteLine("");
                Console.WriteLine("Чтение файла... Для отмены нажмите любую клавишу.");
                Console.ReadKey();
                try
                {
                    s_cts.Cancel();
                    Console.WriteLine("");
                    Console.WriteLine("Чтение файла прервано. Вывод текущих результатов...");
                }
                catch (ObjectDisposedException) { };
            });

            TripletsTop = MostUsedTriplets(FileName, 10);

            string printstr = "";
            foreach (var item in TripletsTop)
            {
                printstr = String.Concat(printstr, item.Key, ", ");
            }
            printstr = printstr.Substring(0, printstr.Length - 2);

            stopwatch.Stop();

            Console.WriteLine("");
            Console.WriteLine("Самые часто встречающиеся триплеты в тексте:");
            Console.WriteLine(printstr);
            Console.WriteLine("Время выполнения программы: {0} мс", stopwatch.Elapsed.TotalMilliseconds);
            Console.ReadLine();
        }
    }
}

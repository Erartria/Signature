using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace Signature
{
    public class SignatureMaker
    {
        private readonly int _bufferSize;
        private readonly string _filepath;
        private readonly string _outPath;
        private readonly int _maxThreads;
        
        private object locker = new object();
        private Mutex mutex = new Mutex();
        
        private long _nOfReadBlock;
        private DateTime _generationTimeStart;
        
        private long StartReadPosition => _nOfReadBlock * _bufferSize;
        private long NOfReadBlock => ++_nOfReadBlock;
        
        public SignatureMaker(string path, int size,int maxNThreads, string outPath = "")
        {
            _filepath = path;
            _bufferSize = size;
            _maxThreads = maxNThreads <= Environment.ProcessorCount
                ? maxNThreads
                : Environment.ProcessorCount;
            _outPath = outPath;
            
            Thread.CurrentThread.Name = "1";
            
            if (!File.Exists(_filepath)) throw new FileNotFoundException("Файл-чтение с указанным путём не найден!");

            if (!_outPath.Equals(""))
            {
                try
                {
                   CreateFile($"Кол-во ядер: {Environment.ProcessorCount}\nКол-во потоков: {_maxThreads}\n");
                }
                catch (DirectoryNotFoundException)
                {
                    throw new DirectoryNotFoundException("Не найдена директория для файла-записи");
                }
            }
        }

        public void Generate()
        {
            _generationTimeStart = DateTime.Now;
            List<Thread> allThreads = new List<Thread>();
            for (var threadNumber = 2; threadNumber <= _maxThreads; ++threadNumber)
            {
                allThreads.Add(new Thread(SubThreadWork) { Name = threadNumber.ToString() });
                allThreads[threadNumber - 2].Start();
            }
            SubThreadWork();
            foreach (var thread in allThreads)
            {
                thread.Join();
            }
            Write($"\nВремя запуска: {_generationTimeStart}\nВремя завершения: {DateTime.Now}");
        }

        public void SubThreadWork()
        {
            for (;;)
            {
                long currentBlock;
                long currentPosition;
                lock (locker)
                {
                    currentPosition = StartReadPosition;
                    currentBlock = NOfReadBlock;
                }

                if (new FileInfo(_filepath).Length < currentPosition)
                    return;

                var buffer = new byte[_bufferSize];
                using (var br = new BinaryReader(File.Open(_filepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    br.BaseStream.Position = currentPosition;
                    br.Read(buffer, 0, _bufferSize);
                }

                SHA256 encrypt = new SHA256Managed();
                var shaHash = encrypt.ComputeHash(buffer);
                var outStr = $"{currentBlock}:\n{ByteArrayToString(shaHash)}";
                Write(outStr);
            }
        }

        private static string ByteArrayToString(byte[] hash) => string.Join(" ", hash.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

        private void Write(string str)
        {
            if (!_outPath.Equals(""))
            {
                AppendToFile(str);
            }
            else
            {
                Console.WriteLine(str);
            }
        }

        private void CreateFile(string initString = "")
        {
            using (var fs = new FileStream(_outPath, FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine(initString);
            }
        }
        private void AppendToFile(string str)
        {
            using (var fs = new FileStream(_outPath, FileMode.Append, FileAccess.Write, FileShare.Write))
            using (var sw = new StreamWriter(fs))
            {
                mutex.WaitOne();
                sw.BaseStream.Seek(0, SeekOrigin.End);
                sw.WriteLine(str); 
                sw.Flush(); 
                sw.BaseStream.Flush(); 
                mutex.ReleaseMutex();
            }
        }
    }
}
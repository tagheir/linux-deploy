﻿using System;
using System.Threading;

namespace ConsoleApp4
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Hello World! -- Naveen Test 0000 --- " + DateTime.Now);
                Thread.Sleep(5000);
            }
        }
    }
}

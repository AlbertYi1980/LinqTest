﻿using System;
using System.Collections.Generic;
using System.Linq;
using MongoLinqs;

namespace ConsoleHost
{
    class Program
    {
        static void Main()
        {
            var dbSet = new MongoDbSet<Student>();
            var q = from s in dbSet
                select s;
            var students = q.ToList();
            PrintResult(students);
        }

        private static void PrintResult(List<Student> students)
        {
            foreach (var student in students)
            {
                Console.WriteLine(student.ToString());
            }
        }
    }
}
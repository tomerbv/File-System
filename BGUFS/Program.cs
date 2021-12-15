using System;

namespace BGUFS
{
    class Program
    {
        static void Main(string[] args)
        {
            int len = args.Length;
            if(len <= 1 || len >= 5)
                Console.WriteLine("Invalid number of aruments");
            else
            {
                string method = args[0];
                string fileSystemName = args[1];
                string arg1 = null, arg2 = null;
                filesystem fs = new filesystem();
                if (len >= 3)
                    arg1 = args[2];
                if (len == 4)
                    arg2 = args[3];
                switch (method)
                {
                    case "-create":
                        if(len != 2)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.create(fileSystemName);
                        break;

                    case "-add":
                        if (len != 3)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.add(fileSystemName, arg1);
                        break;

                    case "-remove":
                        if (len != 3)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.remove(fileSystemName, arg1);
                        break;

                    case "-rename":
                        if (len != 4)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.rename(fileSystemName, arg1, arg2);
                        break;

                    case "-extract":
                        if (len != 4)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.extract(fileSystemName, arg1, arg2);
                        break;

                    case "-dir":
                        if (len != 2)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.dir(fileSystemName);
                        break;

                    case "-hash":
                        if (len != 3)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.hash(fileSystemName, arg1);
                        break;

                    case "-optimize":
                        if (len != 2)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.optimize(fileSystemName);

                        break;

                    case "-sortAB":
                        if (len != 2)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.sortAB(fileSystemName);
                        break;


                    case "-sortDate":
                        if (len != 2)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.sortDate(fileSystemName);
                        break;

                    case "-sortSize":
                        if (len != 2)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.sortSize(fileSystemName);
                        break;

                    case "-addLink":
                        if (len != 4)
                        {
                            Console.WriteLine("Invalid number of aruments");
                            System.Environment.Exit(1);
                        }
                        fs.addLink(fileSystemName, arg1, arg2);
                        break;

                    default:
                        Console.WriteLine("Unknown command: " + method);
                        break;

                }
            }
        }
    }
}

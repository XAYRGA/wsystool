using System;
using System.Collections.Generic;
using System.Text;

namespace xayrga.cmdl
{
    public static class cmdarg
    {
        public static string[] cmdargs;
        public static string assertArg(int argn, string assert)
        {
            if (cmdargs.Length <= argn)
            {
                Console.WriteLine("Missing required argument #{0} for '{1}'", argn, assert);
                Environment.Exit(0);
            }
            return cmdargs[argn];
        }

        public static string findDynamicStringArgument(string name, string def)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i] == name || cmdargs[i] == "-" + name)
                {
                    if (cmdargs.Length >= i + 1)
                        return cmdargs[i + 1];
                    break;
                }
            }
            return def;
        }


        public static int findDynamicNumberArgument(string name, int def)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i] == name || cmdargs[i] == "-" + name)
                {
                    if (cmdargs.Length < i + 1)
                    {
                        int v = 0;
                        var ok = int.TryParse(cmdargs[i + 1], out v);
                        if (!ok)
                        {
                            Console.WriteLine($"Invalid parameter for '{cmdargs[i]}' (Number expected, couldn't parse '{cmdargs[i + 1]}' as a number.)");
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Number argument for '{cmdargs[i]}' expected.");
                    }
                    break;
                }
            }
            return def;
        }

        public static bool findDynamicFlagArgument(string name)
        {
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i] == name || cmdargs[i] == "-" + name)
                {
                    return true;
                }
            }
            return false;
        }

        public static int assertArgNum(int argn, string assert)
        {
            if (cmdargs.Length <= argn)
            {
                Console.WriteLine("Missing required argument #{0} for '{1}'", argn, assert);
                Environment.Exit(0);
            }
            int b = 1;
            var w = int.TryParse(cmdargs[argn], out b);
            if (w == false)
            {
                Console.WriteLine("Cannot parse argument #{0} for '{1}' (expected number, got {2}) ", argn, assert, cmdargs[argn]);
                Environment.Exit(0);
            }
            return b;
        }

        public static string tryArg(int argn, string assert)
        {
            if (cmdargs.Length <= argn)
            {
                if (assert != null)
                {
                    Console.WriteLine("No argument #{0} specified {1}.", argn, assert);
                }
                return null;
            }
            return cmdargs[argn];
        }
        public static void assert(string text, params object[] wtf)
        {
            Console.WriteLine(text, wtf);
            Environment.Exit(0);
        }
        public static void assert(bool cond, string text)
        {
            if (cond == true)
                return;
            Console.WriteLine(text);
            Environment.Exit(0);
        }
    }
}

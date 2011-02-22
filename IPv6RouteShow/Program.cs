namespace IPv6RouteShow
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    internal class Ipv6Route
    {
        public string DestinationPrefix { get; set; }
        public string SourcePrefix { get; set; }
        public int InterfaceIndex { get; set; }
        public string InterfaceName { get; set; }
        public bool Publish { get; set; }
        public string Type { get; set; }
        public int Metric { get; set; }
        public int SitePrefixLength { get; set; }
        public string ValidLifeTime { get; set; }
        public string PreferredLifeTime { get; set; }

        public void SetSitePrefixLength(string s)
        {
            SitePrefixLength = Int32.Parse(s);
        }

        public void SetMetric(string s)
        {
            Metric = Int32.Parse(s);
        }

        public void SetPublish(string s)
        {
            if (s == "Yes")
            {
                s = "True";
            }
            if (s == "No")
            {
                s = "False";
            }

            Publish = Boolean.Parse(s);
        }

        public void SetInterfaceIndex(string s)
        {
            InterfaceIndex = Int32.Parse(s);
        }

        public override string ToString()
        {
            return DestinationPrefix + " via " + InterfaceName;
        }
    }

    class Program
    {
        private static List<Ipv6Route> GetIPv6RouteOutput()
        {
            // Start the child process.
            var p = new Process
                        {
                            StartInfo =
                                {
                                    Arguments = "interface ipv6 show route verbose",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    FileName = "netsh.exe"
                                }
                        };
            p.Start();
            var routes = ParseRoutes(p.StandardOutput);
            p.WaitForExit();

            return routes;
        }

        private static KeyValuePair<string, string>? ScanLabel(string line)
        {
            int index = 0;
            int whitespace = 0;

            foreach(char c in line)
            {
                // We are looking for two consecutive occurrences of whitespace. 
                // ':' counts as whitespace. (the string ": ")
                if (c == ' ' || c == ':')
                {
                    whitespace++;
                }
                else
                {
                    whitespace = 0;
                }

                if(whitespace >= 2)
                {
                    return new KeyValuePair<string, string>(
                        line.Substring(0, index).Trim(),
                        line.Substring(index + 1).Trim());
                }
                index++;
            }

            return null;
        }

        private static List<Ipv6Route> ParseRoutes(StreamReader netshOutput)
        {
            string line;
            bool routeHasData = false;
            int blankLines = 0;
            Ipv6Route route = new Ipv6Route();
            var routes = new List<Ipv6Route>();

            while((line = netshOutput.ReadLine()) != null)
            {
                if(line.Trim().Equals(""))
                {
                    blankLines++;
                    if(blankLines == 2)
                    {
                        routes.Add(route);
                        route = new Ipv6Route();
                        routeHasData = false;
                    }
                }
                else
                {
                    blankLines = 0;
                }

                var mapper = new Dictionary<string, Action<Ipv6Route, string>>
                                 {
                                     {"Destination Prefix:",     (r, s) => r.DestinationPrefix = s},
                                     {"Source Prefix:",          (r, s) => r.SourcePrefix = s},
                                     {"Interface Index:",        (r, s) => r.SetInterfaceIndex(s)},
                                     {"Gateway/Interface Name:", (r, s) => r.InterfaceName = s},
                                     {"Publish:",                (r, s) => r.SetPublish(s)},
                                     {"Type:",                   (r, s) => r.Type = s},
                                     {"Metric:",                 (r, s) => r.SetMetric(s)},
                                     {"SitePrefixLength",        (r, s) => r.SetSitePrefixLength(s)},
                                     {"ValidLifeTime",           (r, s) => r.ValidLifeTime = s},
                                     {"PreferredLifeTime",       (r, s) => r.PreferredLifeTime = s},
                                 };

                var nullbleKvp = ScanLabel(line);
                if (nullbleKvp == null)
                {
                    continue;
                }

                var kvp = nullbleKvp.Value;
                var label = kvp.Key;
                var value = kvp.Value;
                
                if(mapper.ContainsKey(label))
                {
                    var action = mapper[label];
                    action.Invoke(route, value);
                    routeHasData = true;
                }
            }
            if(routeHasData)
            {
                routes.Add(route);
            }

            return routes;
        }

        static void Main(string[] args)
        {
            var routes = GetIPv6RouteOutput();
            foreach(var route in routes)
            {
                Console.WriteLine(route);
            }
        }
    }
}

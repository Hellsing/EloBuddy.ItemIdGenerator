using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace EloBuddy.ItemIdEnumGenerator
{
    public static class Program
    {
        #region Constant Data

        public const string DashPattern = @"( |-)";
        public const string BlankPattern = @"(\(|\)|'|:|\.)";

        public static readonly int[] InvalidItems =
        {
            3671, // Caulfields_Warhammer_Enchantment_Warrior
            3672, // Bamis_Cinder_Enchantment_Cinderhulk
            3673, // Amplifying_Tome_Enchantment_Runic_Echoes
            3674 // Recurve_Bow_Enchantment_Devourer
        };

        public static readonly Dictionary<int, string> DuplicateNames = new Dictionary<int, string>
        {
            { 3074, "Ravenous_Hydra_Melee_Only" },
            { 3077, "Tiamat_Melee_Only" },
            { 3085, "Runaans_Hurricane_Ranged_Only" }
        };

        public const string Header =
            @"using System;

            namespace EloBuddy
            {
                public enum ItemId
                {
                    Unknown = 0,";

        public const string Footer =
                @"    }
            }";

        public const string IndentString = "        ";

        #endregion

        public static void Main(string[] args)
        {
            using (var wc = new WebClient())
            {
                // Get latest data version
                string latestVersion;
                try
                {
                    latestVersion = JsonConvert.DeserializeObject<string[]>(wc.DownloadString("https://ddragon.leagueoflegends.com/api/versions.json"))[0];
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to gather most recent data version, exiting!");
                    Console.ReadKey();
                    return;
                }

                // Get item data json string
                string itemData;
                try
                {
                    itemData = wc.DownloadString($"http://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/en_US/item.json");
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to download item data, exiting!");
                    Console.ReadKey();
                    return;
                }

                // Parse item data json string
                ItemJson items;
                try
                {
                    items = JsonConvert.DeserializeObject<ItemJson>(itemData);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to parse json, exiting!");
                    Console.ReadKey();
                    return;
                }

                // Prepare the string builder for the output
                var sb = new StringBuilder();
                sb.AppendLine(Header);

                // Parsed items are strored in here
                var itemsParsed = new Dictionary<string, int>();

                foreach (var entry in items.data)
                {
                    // Skip invalid item ids
                    if (InvalidItems.Contains(entry.Key))
                    {
                        continue;
                    }

                    var preItem = "";
                    var name = PrepareStringForEnum(entry.Value.name);

                    // Handle enchantments
                    if (name.ToLower().Contains("enchantment"))
                    {
                        if (entry.Value.from == null)
                        {
                            continue;
                        }

                        var currentItem = entry.Value;
                        do
                        {
                            currentItem = items.data.FirstOrDefault(o => o.Key == currentItem.from.Last()).Value;
                            preItem = PrepareStringForEnum(currentItem.name);
                        } while (preItem.ToLower().Contains("enchantment") && currentItem.from != null);

                        preItem += "_";
                    }

                    // Add parsed item to dictionary
                    itemsParsed[preItem + name] = entry.Key;

                    // Check for required duplicates (like "Melee Only")
                    if (DuplicateNames.ContainsKey(entry.Key))
                    {
                        itemsParsed[DuplicateNames[entry.Key]] = entry.Key;
                    }
                }

                // Sort parsed items by name
                itemsParsed = itemsParsed.OrderBy(o => o.Key).ToDictionary(o => o.Key, o => o.Value);

                // Add parsed items to the string builder
                foreach (var entry in itemsParsed)
                {
                    sb.Append(IndentString);
                    sb.Append(entry.Key);
                    sb.Append(" = ");
                    sb.Append(entry.Value);
                    sb.AppendLine(",");
                }

                // Add final footer to the string builder
                sb.AppendLine(Footer);

                // Write string builder content to file
                File.WriteAllText("ItemId.cs", sb.ToString());
            }
        }

        public static string PrepareStringForEnum(string input)
        {
            return Regex.Replace(Regex.Replace(input, BlankPattern, ""), DashPattern, "_");
        }
    }
}

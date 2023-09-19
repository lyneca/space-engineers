using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string CONFIG_BLOCK_NAME = "z.RefineryDemandBalancer";
        string[] REFINERY_BLOCK_NAMES = {
            ". Mothership Refinery A",
            ". Mothership Refinery B",
            ". Mothership Refinery C",
            ". Mothership Refinery D",
            "Refinery",
            "Refinery 2",
            "Refinery 3",
            "Refinery 4"
        };

        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        class InventoryTarget
        {
            public string DisplayName;
            public string RawItemType;
            public string ProcessedItemType;
            public decimal DesiredProcessedAmount;
            public decimal ProcessingConversionFactor;
        }

        class InventoryTargetParser
        {
            private IMyTerminalBlock ReadFromBlock { get; set; }

            private List<InventoryTarget> inventoryTargets = new List<InventoryTarget>();

            public List<InventoryTarget> InventoryTargets { get { return inventoryTargets; } }

            public InventoryTargetParser(IMyTerminalBlock readFromBlock)
            {
                if (readFromBlock == null) throw new Exception("readFromBlock is null");
                ReadFromBlock = readFromBlock;
            }

            public List<InventoryTarget> ParseInventoryTargets()
            {
                inventoryTargets.Clear();
                string[] lines = ReadFromBlock.CustomData.Split('\n');

                foreach (string line in lines)
                {
                    string[] fields = line.Split(',');
                    inventoryTargets.Add(new InventoryTarget
                    {
                        DisplayName = fields[0],
                        RawItemType = $"Ore/{fields[0]}",
                        ProcessedItemType = $"Ingot/{fields[0]}",
                        ProcessingConversionFactor = Convert.ToDecimal(fields[1]),
                        DesiredProcessedAmount = int.Parse(fields[2])
                    });
                }

                return InventoryTargets;
            }
        }

        Dictionary<string, MyFixedPoint> GetItemAmounts(List<InventoryTarget> inventoryTargets)
        {
            var inventoryBlocks = new List<IMyInventoryOwner>();
            GridTerminalSystem.GetBlocksOfType(inventoryBlocks);

            Dictionary<string, MyFixedPoint> itemCounts = new Dictionary<string, MyFixedPoint>();

            inventoryBlocks.ForEach(inventoryBlock =>
            {
                for (int inventoryIndex = 0; inventoryIndex < inventoryBlock.InventoryCount; inventoryIndex++)
                {
                    List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
                    inventoryBlock.GetInventory(inventoryIndex).GetItems(inventoryItems);

                    inventoryItems.ForEach(item =>
                    {
                        string type = item.Type.ToString().Split('_')[1];
                        if (!itemCounts.ContainsKey(type)) itemCounts[type] = 0;
                        itemCounts[type] += item.Amount;
                    });
                }
            });

            foreach (var inventoryTarget in inventoryTargetParser.InventoryTargets)
            {
                decimal rawCount = decimal.Parse(itemCounts.ContainsKey(inventoryTarget.RawItemType)
                    ? itemCounts[inventoryTarget.RawItemType].ToString()
                    : "0");
                decimal processedCount = decimal.Parse(itemCounts.ContainsKey(inventoryTarget.ProcessedItemType)
                    ? itemCounts[inventoryTarget.ProcessedItemType].ToString()
                    : "0");
                decimal rawCountWithFactor = rawCount * inventoryTarget.ProcessingConversionFactor;
                decimal ratioRaw = rawCountWithFactor / inventoryTarget.DesiredProcessedAmount;
                decimal ratioProcessed = processedCount / inventoryTarget.DesiredProcessedAmount;
                decimal afterProcessing = processedCount + rawCountWithFactor;

                char EmptyPip = '░';
                char FullPip = '█';

                string bar = "";
                int charCount = 40;
                for (decimal i = 0; i < charCount; i++)
                {
                    decimal cur_ratio = i / charCount;
                    if (cur_ratio <= ratioProcessed)
                    {
                        bar += FullPip;
                    }
                    else if (cur_ratio <= ratioProcessed + ratioRaw)
                    {
                        bar += EmptyPip;
                    }
                    else
                    {
                        bar += ".";
                    }
                }
                decimal percent = (ratioRaw + ratioProcessed) * 100;
            }

            return itemCounts;
        }

        InventoryTargetParser inventoryTargetParser;

        /**
         * Make the sum of all values equal 1.
         */
        Dictionary<string, decimal> NormaliseValues(Dictionary<string, decimal> inputValues)
        {
            decimal min = decimal.MaxValue;
            decimal max = decimal.MinValue;
            foreach (var value in inputValues.Values)
            {
                min = value < min ? value : min;
                max = value > max ? value : max;
            }

            Dictionary<string, decimal> normalisedValues = new Dictionary<string, decimal>();
            foreach (var entry in inputValues)
            {
                // Qwik mafs
                normalisedValues[entry.Key] = (entry.Value - min) / (max - min);
            }

            decimal sum = 0;
            foreach (var value in normalisedValues.Values)
            {
                sum += value;
            }

            normalisedValues = normalisedValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value / sum);

            return normalisedValues;
        }

        Dictionary<string, decimal> GetWeights(Dictionary<string, decimal> ingotTargetRatios)
        {
            // Reciprocate all ratios.
            Dictionary<string, decimal> weights = ingotTargetRatios.ToDictionary(kvp => kvp.Key, kvp => 1 - kvp.Value);

            // weights = NormaliseValues(weights);

            decimal sum = weights.Values.Where(value => value > 0).Sum();
            weights = weights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value / sum);

            return weights;
        }

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            var configBlock = GridTerminalSystem.GetBlockWithName(CONFIG_BLOCK_NAME);
            inventoryTargetParser = new InventoryTargetParser(configBlock);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            inventoryTargetParser.ParseInventoryTargets();

            // 0 is no ore, 1 means exactly at target.
            var ingotTargetRatios = new Dictionary<string, decimal>();

            var itemAmounts = GetItemAmounts(inventoryTargetParser.InventoryTargets);

            foreach (var inventoryTarget in inventoryTargetParser.InventoryTargets)
            {
                // Get the ingot amount.
                var ingotAmount = itemAmounts[inventoryTarget.ProcessedItemType];

                // Get the target amount.
                var targetAmount = inventoryTarget.DesiredProcessedAmount;

                // Qwik mafs.
                ingotTargetRatios[inventoryTarget.DisplayName] = ((decimal)ingotAmount) / targetAmount;
            }

            var weights = GetWeights(ingotTargetRatios);

            List<KeyValuePair<string, decimal>> normalisedList = weights.ToList();

            normalisedList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

            foreach(var pair in normalisedList)
            {
                Echo($"{pair.Key}: {pair.Value}");
            }

            // TODO: Actually assign refining operations.
        }
    }
}

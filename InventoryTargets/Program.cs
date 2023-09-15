using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
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
        /*
         * R e a d m e
         * -----------
         * 
         * In this file you can include any instructions or other comments you want to have injected onto the 
         * top of your final script. You can safely delete this file if you do not want any such comments.
         */

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

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Me.CustomData = "";
            var inventoryBlocks = new List<IMyInventoryOwner>();
            GridTerminalSystem.GetBlocksOfType(inventoryBlocks);

            Dictionary<string, MyFixedPoint> itemCounts = new Dictionary<string, MyFixedPoint>();

            inventoryBlocks.ForEach(inventoryBlock => {
                for (int inventoryIndex = 0; inventoryIndex < inventoryBlock.InventoryCount; inventoryIndex++)
                {
                    List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
                    inventoryBlock.GetInventory(inventoryIndex).GetItems(inventoryItems);

                    inventoryItems.ForEach(item => {
                        string type = item.Type.ToString().Split('_')[1];
                        if (!itemCounts.ContainsKey(type)) itemCounts[type] = 0;
                        itemCounts[type] += item.Amount;
                    });
                }
            });

            foreach (var inventoryTarget in inventoryTargets)
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
                Me.CustomData += $"{inventoryTarget.DisplayName} ";
                Me.CustomData += $"{processedCount.ToString("00.00")}kg - {rawCount.ToString("00.00")}kg raw - [{afterProcessing.ToString("00.00")}kg]\n";

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
                Me.CustomData += $"{bar} [{percent.ToString("00.00")}%]\n";
            }
        }

        List<InventoryTarget> inventoryTargets = new List<InventoryTarget>() {
            new InventoryTarget() {
                DisplayName = "Iron",
                RawItemType = "Ore/Iron",
                ProcessedItemType = "Ingot/Iron",
                DesiredProcessedAmount = 100000,
                ProcessingConversionFactor = 0.70M,
            },
            new InventoryTarget() {
                DisplayName = "Silicon",
                RawItemType = "Ore/Silicon",
                ProcessedItemType = "Ingot/Silicon",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.70M,
            },
            new InventoryTarget() {
                DisplayName = "Nickel",
                RawItemType = "Ore/Nickel",
                ProcessedItemType = "Ingot/Nickel",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.40M,
            },
            new InventoryTarget() {
                DisplayName = "Cobalt",
                RawItemType = "Ore/Cobalt",
                ProcessedItemType = "Ingot/Cobalt",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.30M,
            },
            new InventoryTarget() {
                DisplayName = "Silver",
                RawItemType = "Ore/Silver",
                ProcessedItemType = "Ingot/Silver",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.10M,
            },
            new InventoryTarget() {
                DisplayName = "Gold",
                RawItemType = "Ore/Gold",
                ProcessedItemType = "Ingot/Gold",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.01M,
            },
            new InventoryTarget() {
                DisplayName = "Uranium",
                RawItemType = "Ore/Uranium",
                ProcessedItemType = "Ingot/Uranium",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.01M,
            },

            new InventoryTarget() {
                DisplayName = "Magnesium",
                RawItemType = "Ore/Magnesium",
                ProcessedItemType = "Ingot/Magnesium",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.007M,
            },
            new InventoryTarget() {
                DisplayName = "Platinum",
                RawItemType = "Ore/Platinum",
                ProcessedItemType = "Ingot/Platinum",
                DesiredProcessedAmount = 5000,
                ProcessingConversionFactor = 0.005M,
            },
        };
    }

    class InventoryTarget
    {
        public string DisplayName;
        public string RawItemType;
        public string ProcessedItemType;
        public decimal DesiredProcessedAmount;
        public decimal ProcessingConversionFactor;
    }



    class ProgressBar
    {
        public ProgressBar()
        {
            BarWidth = 10;
            Current = 0;
            Total = 10;
        }

        public ProgressBar(int width) : this()
        {
            BarWidth = width;
        }

        public ProgressBar(Func<double> evaluator, int width = 10) : this()
        {
            this.evaluator = evaluator;
            BarWidth = width;
        }

        public Func<double> evaluator;
        public int BarWidth { get; set; }
        public double Current { get; set; }
        public double Total { get; set; }

        public double Ratio
        {
            get
            {
                return Current / Total;
            }
            set
            {
                Current = Total * value;
            }
        }

        public string Percentage
        {
            get
            {
                string unpadded = $"{Current / Total * 100:0}%";
                return unpadded.PadLeft(4);
            }
        }

        private const char EmptyPip = '░';
        private const char FullPip = '█';

        public string Render(bool showPercentage = false)
        {
            if (BarWidth == 0 || Total == 0) return "";
            if (evaluator != null) Ratio = evaluator();

            var filledPips = (int)Math.Round(Current / Total * BarWidth);

            var renderedBar = "";

            for (var pip = 0; pip < BarWidth; pip++)
            {
                renderedBar += pip < filledPips
                    ? FullPip
                    : EmptyPip;
            }

            if (showPercentage)
            {
                renderedBar = $"{renderedBar} {Percentage}";
            }

            return renderedBar;
        }

        public override string ToString()
        {
            return Render(true);
        }
    }
}

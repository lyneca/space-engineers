using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
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
        /**
         * This script automatically parses a text representation of many InventoryTarget
         * instances into actual List<InventoryTarget>.
         * 
         * The expected representation of the text file is a CSV in the format:
         * Metal,ConversionRatio,DesiredTarget
         */

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
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            var configBlock = GridTerminalSystem.GetBlockWithName("z.RefineryDemandBalancer");
            var inventoryTargetParser = new InventoryTargetParser(configBlock);

            inventoryTargetParser.ParseInventoryTargets();

            Echo(inventoryTargetParser.InventoryTargets.First().DisplayName);
            Echo(inventoryTargetParser.InventoryTargets.First().RawItemType);
            Echo(inventoryTargetParser.InventoryTargets.First().ProcessedItemType);
            Echo(inventoryTargetParser.InventoryTargets.First().ProcessingConversionFactor.ToString());
            Echo(inventoryTargetParser.InventoryTargets.First().DesiredProcessedAmount.ToString());
        }
    }
}

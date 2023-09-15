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
using System.Text.RegularExpressions;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /**
         * Populate these two arrays.
         */

        string[] massDisplayScreens = {
            "Storage Monitor Display"
        };

        string[] volumeDisplayScreens =
        {
            "Volume Monitor Display"
        };

        /////////////////////////////////


        class BlockInventoryInfo
        {
            public BlockInventoryInfo()
            {
                Mass = 0;
                Volume = 0;
            }

            public MyFixedPoint Mass { get; set; } // kg
            public MyFixedPoint Volume { get; set; } // m^3

            public string MassString
            {
                get
                {
                    return $"{Mass:0.##}kg";
                }
            }

            // Returns Litres
            public string VolumeString
            {
                get
                {
                    return $"{Volume * 1000:0.##}L";
                }
            }
        }

        class GasInfo
        {
            public GasInfo()
            {
                Volume = 0;
                Capacity = 0;
            }

            public double Volume { get; set; }
            public double Capacity { get; set; }

            public double Usage
            {
                get
                {
                    return Capacity != 0
                        ? Volume / Capacity
                        : 0;
                }
            }

            public string VolumeString
            {
                get
                {
                    return $"{Volume:0.##}L";
                }
            }

            public string CapacityString
            {
                get
                {
                    return $"{Capacity:0.##}L";
                }
            }

            public string UsageString
            {
                get
                {
                    return $"{Usage * 100:0.##}%";
                }
            }
        }

        public enum DisplayType
        {
            Mass,
            Volume,
        }

        class InventoryDisplay
        {
            private IMyTextPanel textPanel { get; set; }
            private DisplayType displayType;

            public InventoryDisplay(IMyTextPanel textPanel, DisplayType displayType)
            {
                this.textPanel = textPanel;
                this.displayType = displayType;
            }

            public void RenderInventoryInfo(
                Dictionary<string, BlockInventoryInfo> blockCounts,
                BlockInventoryInfo ice,
                Dictionary<string, GasInfo> gasInfos = null)
            {
                List<string> lines = (new[] {
                    "Ingots:",
                    string.Join("\n", blockCounts.Select(kvp => $" - {kvp.Key}: {(displayType == DisplayType.Mass ? kvp.Value.MassString : kvp.Value.VolumeString)}")),
                    $"Ice: {(displayType == DisplayType.Mass ? ice.MassString : ice.VolumeString)}"
                }).ToList();

                // Gas readouts
                if (gasInfos != null && displayType == DisplayType.Volume)
                {
                    var oxygen = gasInfos["Oxygen"];
                    var hydrogen = gasInfos["Hydrogen"];
                    lines.Add($"Oxygen: {oxygen.VolumeString}/{oxygen.CapacityString} ({oxygen.UsageString})");
                    lines.Add($"Hydrogen: {hydrogen.VolumeString}/{hydrogen.CapacityString} ({hydrogen.UsageString})");
                }

                textPanel.WriteText(string.Join("\n", lines));


            }
        }

        List<InventoryDisplay> inventoryDisplays;

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
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            if (inventoryDisplays == null)
            {
                inventoryDisplays = new List<InventoryDisplay>();
            }

            foreach (var blockName in massDisplayScreens)
            {
                IMyTextPanel textPanel = (GridTerminalSystem.GetBlockWithName(blockName) as IMyTextPanel);
                if (textPanel == null) continue;

                inventoryDisplays.Add(new InventoryDisplay(textPanel, DisplayType.Mass));
            }

            foreach (var blockName in volumeDisplayScreens)
            {
                IMyTextPanel textPanel = (GridTerminalSystem.GetBlockWithName(blockName) as IMyTextPanel);
                if (textPanel == null) continue;

                inventoryDisplays.Add(new InventoryDisplay(textPanel, DisplayType.Volume));
            }
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
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            var light = GridTerminalSystem.GetBlockWithName("Ice Warning Light") as IMyTextPanel;
            var inventoryBlocks = new List<IMyInventoryOwner>();
            GridTerminalSystem.GetBlocksOfType(inventoryBlocks);
            var blockCounts = new Dictionary<string, BlockInventoryInfo>();
            BlockInventoryInfo ice = new BlockInventoryInfo();
            inventoryBlocks.ForEach(block => {
                for (var i = 0; i < block.InventoryCount; i++)
                {
                    var items = new List<MyInventoryItem>();
                    block.GetInventory(i).GetItems(items);
                    items.ForEach(item => {
                        float unitVolume;
                        MyFixedPoint volume;

                        if (item.Type.SubtypeId == "Ice")
                        {
                            ice.Mass = MyFixedPoint.AddSafe(ice.Mass, item.Amount);
                            unitVolume = item.Type.GetItemInfo().Volume;
                            volume = MyFixedPoint.MultiplySafe(unitVolume, item.Amount);
                            ice.Volume = MyFixedPoint.AddSafe(ice.Volume, volume);
                        }
                        if (item.Type.TypeId != "MyObjectBuilder_Ingot") return;

                        if (!blockCounts.ContainsKey(item.Type.SubtypeId))
                        {
                            blockCounts[item.Type.SubtypeId] = new BlockInventoryInfo();
                        }

                        blockCounts[item.Type.SubtypeId].Mass = MyFixedPoint.AddSafe(blockCounts[item.Type.SubtypeId].Mass, item.Amount);

                        // Volume calculations
                        unitVolume = item.Type.GetItemInfo().Volume;
                        volume = MyFixedPoint.MultiplySafe(unitVolume, item.Amount);
                        blockCounts[item.Type.SubtypeId].Volume = MyFixedPoint.AddSafe(blockCounts[item.Type.SubtypeId].Volume, volume);
                    });
                }
            });

            // Calculate Gas Storage
            var gasInfos = new Dictionary<string, GasInfo>();
            gasInfos["Hydrogen"] = new GasInfo();
            gasInfos["Oxygen"] = new GasInfo();
            var gasTanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(gasTanks);

            gasTanks.ForEach(gasTank =>
            {
                string gas = "";
                if (gasTank.DisplayNameText.Contains("Oxygen"))
                {
                    gas = "Oxygen";
                }
                else if (gasTank.DisplayNameText.Contains("Hydrogen"))
                {
                    gas = "Hydrogen";
                }
                else
                {
                    // Skip weirdos.
                    return;
                }

                double tankUsedVolume = gasTank.Capacity * gasTank.FilledRatio;
                gasInfos[gas].Volume += tankUsedVolume;
                gasInfos[gas].Capacity += gasTank.Capacity;
            });

            // Ice warning light
            if (light != null)
            {
                if (ice.Mass < 1000)
                {
                    light.BackgroundColor = Color.Red;
                    light.WriteText("OUT OF ICE");
                }
                else if (ice.Mass < 10000)
                {
                    light.BackgroundColor = Color.Orange;
                    light.WriteText("LOW ICE WARNING");
                }
                else
                {
                    light.BackgroundColor = Color.Green;
                    light.WriteText("ICE OKAY");
                }
            }

            foreach (var display in inventoryDisplays)
            {
                display.RenderInventoryInfo(blockCounts, ice, gasInfos);
            }
        }

    }
}

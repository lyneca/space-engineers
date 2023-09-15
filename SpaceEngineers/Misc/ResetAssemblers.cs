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
using Sandbox.Game.GameSystems;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Groups;
using VRageMath;

namespace IngameScript {
    partial class ResetProductionBlocks : MyGridProgram {
        public Program() { }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource) {
            var assemblers = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType(assemblers);

            var refineries = new List<IMyRefinery>();
            GridTerminalSystem.GetBlocksOfType(refineries);

            var cargoGroup = GridTerminalSystem.GetBlockGroupWithName("Mothership Cargo");
            if (cargoGroup == null) return;

            var containers = new List<IMyCargoContainer>();
            cargoGroup.GetBlocksOfType(containers);
            if (containers.Count == 0) return;

            var moved = 0;

            foreach (var assembler in assemblers) {
                for (var i = 0; i < assembler.InputInventory.ItemCount; i++) {
                    foreach (var container in containers) {
                        if (assembler.InputInventory.TransferItemTo(container.GetInventory(), i, stackIfPossible: true))
                            break;
                    }

                    moved++;
                }
            }

            foreach (var refinery in refineries) {
                for (var i = 0; i < refinery.OutputInventory.ItemCount; i++) {
                    foreach (var container in containers) {
                        if (refinery.OutputInventory.TransferItemTo(container.GetInventory(), i, stackIfPossible: true))
                            break;
                    }
                    
                    moved++;
                }
            }

            Echo($"Moved {moved} items from refineries and assemblies to cargo storage.");
        }
    }
}
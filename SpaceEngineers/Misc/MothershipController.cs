using System;
using System.Collections.Generic;
using EmptyKeys.UserInterface.Generated.RespawnScreen_Bindings;
using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    public class Program : MyGridProgram {
        class ProgressBar {
            public ProgressBar() {
                BarWidth = 10;
                Current = 0;
                Total = 10;
            }

            public ProgressBar(int width) : this() {
                BarWidth = width;
            }

            public ProgressBar(Func<double> evaluator, int width = 10) : this() {
                this.evaluator = evaluator;
                BarWidth = width;
            }

            public Func<double> evaluator;
            public int BarWidth { get; set; }
            public double Current { get; set; }
            public double Total { get; set; }

            public double Ratio {
                get {
                    return Current / Total;
                }
                set {
                    Current = Total * value;
                }
            }

            public string Percentage {
                get {
                    string unpadded = $"{Current / Total * 100:0}%";
                    return unpadded.PadLeft(4);
                }
            }

            private const char EmptyPip = '░';
            private const char FullPip = '█';

            public string Render(bool showPercentage = false) {
                if (BarWidth == 0 || Total == 0) return "";
                if (evaluator != null) Ratio = evaluator();

                var filledPips = (int)Math.Round(Current / Total * BarWidth);

                var renderedBar = "";

                for (var pip = 0; pip < BarWidth; pip++) {
                    renderedBar += pip < filledPips
                        ? FullPip
                        : EmptyPip;
                }

                if (showPercentage) {
                    renderedBar = $"{renderedBar} {Percentage}";
                }

                return renderedBar;
            }

            public override string ToString() {
                return Render(true);
            }
        }

        public Program() {
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

            jumpDriveChargeBar = new ProgressBar(() => {
                var jumpDrives = new List<IMyJumpDrive>();
                GridTerminalSystem.GetBlocksOfType(jumpDrives);
                float current = 0;
                float max = 0;

                foreach (var drive in jumpDrives) {
                    current += drive.CurrentStoredPower;
                    max += drive.MaxStoredPower;
                }

                return current / max;
            }, 15);
            
            oxygenBar = new ProgressBar(() => {
                var tanks = new List<IMyGasTank>();
                GridTerminalSystem.GetBlocksOfType(tanks);
                double current = 0, max = 0;
                foreach (var tank in tanks) {
                    if (tank.DisplayNameText == null) continue;
                    if (!tank.DisplayNameText.Contains("Oxygen")) continue;
                    current += tank.Capacity * tank.FilledRatio;
                    max += tank.Capacity;
                }

                if (max == 0) return 0;
                return current / max;
            }, 15);
            
            hydrogenBar = new ProgressBar(() => {
                var tanks = new List<IMyGasTank>();
                GridTerminalSystem.GetBlocksOfType(tanks);
                double current = 0, max = 0;
                foreach (var tank in tanks) {
                    if (tank.DisplayNameText == null) continue;
                    if (!tank.DisplayNameText.Contains("Hydrogen")) continue;
                    current += tank.Capacity * tank.FilledRatio;
                    max += tank.Capacity;
                }

                if (max == 0) return 0;
                return current / max;
            }, 15);
            
            powerBar = new ProgressBar(() => {
                var batteries = new List<IMyBatteryBlock>();
                GridTerminalSystem.GetBlocksOfType(batteries);
                double current = 0, max = 0;
                foreach (var battery in batteries) {
                    current += battery.CurrentStoredPower;
                    max += battery.MaxStoredPower;
                }

                if (max == 0) return 0;
                return current / max;
            }, 15);

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save() {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        private ProgressBar jumpDriveChargeBar;
        private ProgressBar hydrogenBar;
        private ProgressBar oxygenBar;
        private ProgressBar powerBar;

        public void Main(string argument, UpdateType updateSource) {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            var output = new[] {
                $"PWR: {powerBar}",
                $"HYD: {hydrogenBar}",
                $"OXY: {oxygenBar}",
                $"JMP: {jumpDriveChargeBar}"
            };

            Me.CustomData = string.Join("\n", output);
            Me.GetSurface(0).WriteText(Me.CustomData);
            foreach (string displayName in argument.Split(',')) {
                var block = GridTerminalSystem.GetBlockWithName(displayName);
                (block as IMyTextSurfaceProvider)?.GetSurface(0).WriteText(Me.CustomData);
                (block as IMyTextSurface)?.WriteText(Me.CustomData);
            }
        }
    }
}
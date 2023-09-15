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

        public Program()
        {
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
        }
    }
}

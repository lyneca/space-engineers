using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI;

namespace IngameScript {
    partial class Program : MyGridProgram {
        public IMyCockpit cockpit;
        public IMyProjector projector;

        public Program() {
            cockpit = GridTerminalSystem.GetBlockWithName("EXP3 Cockpit") as IMyCockpit;
            projector = GridTerminalSystem.GetBlockWithName("EXP3 Status Projector") as IMyProjector;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        public void Save() {}

        public void Main(string argument, UpdateType updateSource) {
            projector.Enabled = cockpit.IsUnderControl;
        }
    }
}


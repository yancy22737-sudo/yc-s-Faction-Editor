using System.Collections.Generic;
using System.Linq;

namespace FactionGearCustomizer
{
    public class BatchUndoable : IUndoable
    {
        private List<KindGearData> targets;
        private string contextId;

        public BatchUndoable(List<KindGearData> targets, string contextId = "BatchApply")
        {
            this.targets = targets;
            this.contextId = contextId;
        }

        public string ContextId => contextId;

        public object CreateSnapshot()
        {
            // Snapshot is a list of deep copies of all targets
            var snapshots = new List<KindGearData>(targets.Count);
            foreach (var target in targets)
            {
                snapshots.Add(target.DeepCopy());
            }
            return snapshots;
        }

        public void RestoreFromSnapshot(object snapshot)
        {
            var snapshots = snapshot as List<KindGearData>;
            if (snapshots == null || snapshots.Count != targets.Count) return;

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].CopyFrom(snapshots[i]);
            }
        }
    }
}

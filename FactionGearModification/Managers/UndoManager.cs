using System;
using System.Collections.Generic;
using Verse;

namespace FactionGearCustomizer
{
    public static class UndoManager
    {
        private const int MaxUndoSteps = 30;
        
        private struct UndoStep
        {
            public IUndoable Target;
            public object Snapshot;
        }

        private static LinkedList<UndoStep> undoList = new LinkedList<UndoStep>();
        private static Stack<UndoStep> redoStack = new Stack<UndoStep>();
        private static readonly object lockObj = new object();

        private static string currentContextId = null;

        public static bool CanUndo
        {
            get
            {
                lock (lockObj)
                {
                    return undoList.Count > 0;
                }
            }
        }

        public static bool CanRedo
        {
            get
            {
                lock (lockObj)
                {
                    return redoStack.Count > 0;
                }
            }
        }

        public static void RecordState(IUndoable target)
        {
            if (target == null) return;

            lock (lockObj)
            {
                // If context changed, clear stacks
                if (currentContextId != target.ContextId)
                {
                    ClearInternal();
                    currentContextId = target.ContextId;
                }

                // Create a snapshot
                object snapshot = target.CreateSnapshot();
                
                // Add to undo list
                undoList.AddLast(new UndoStep { Target = target, Snapshot = snapshot });
                
                if (undoList.Count > MaxUndoSteps)
                {
                    undoList.RemoveFirst();
                }

                redoStack.Clear();
            }
        }

        public static void Undo()
        {
            lock (lockObj)
            {
                if (undoList.Count == 0) return;

                UndoStep last = undoList.Last.Value;
                
                // Check context
                if (currentContextId != last.Target.ContextId)
                {
                    ClearInternal();
                    return;
                }

                // Save current state to Redo stack before restoring
                redoStack.Push(new UndoStep { 
                    Target = last.Target, 
                    Snapshot = last.Target.CreateSnapshot() 
                });

                // Restore
                undoList.RemoveLast();
                last.Target.RestoreFromSnapshot(last.Snapshot);
            }
        }

        public static void Redo()
        {
            lock (lockObj)
            {
                if (redoStack.Count == 0) return;

                UndoStep next = redoStack.Pop();

                // Check context
                if (currentContextId != next.Target.ContextId)
                {
                    ClearInternal();
                    return;
                }

                // Save current state to Undo stack
                undoList.AddLast(new UndoStep { 
                    Target = next.Target, 
                    Snapshot = next.Target.CreateSnapshot() 
                });
                
                if (undoList.Count > MaxUndoSteps) undoList.RemoveFirst();

                // Restore
                next.Target.RestoreFromSnapshot(next.Snapshot);
            }
        }

        public static void Clear()
        {
            lock (lockObj)
            {
                ClearInternal();
            }
        }

        private static void ClearInternal()
        {
            undoList.Clear();
            redoStack.Clear();
            currentContextId = null;
        }
    }
}

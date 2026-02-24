namespace FactionGearCustomizer
{
    public interface IUndoable
    {
        string ContextId { get; }
        object CreateSnapshot();
        void RestoreFromSnapshot(object snapshot);
    }
}

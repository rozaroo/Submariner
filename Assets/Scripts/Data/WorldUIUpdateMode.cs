
public enum WorldUIUpdateMode
{
    Static,
    Dynamic,
    Interval //TODO: Temporal, needs to be implemented, will update the UI every x seconds, instead of every frame, to save performance. Maybe separate coroutines?
}

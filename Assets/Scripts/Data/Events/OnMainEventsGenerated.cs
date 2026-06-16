using System.Collections.Generic;

public struct OnMainEventsGenerated : IGameEvent
{
    public List<MainWorldEvent> MainEvents;
    public ExtractionPointElement ExtractionPoint;

    public OnMainEventsGenerated(List<MainWorldEvent> mainEvents, ExtractionPointElement extractionPoint)
    {
        MainEvents = mainEvents;
        ExtractionPoint = extractionPoint;
    }
}
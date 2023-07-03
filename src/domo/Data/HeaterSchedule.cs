﻿using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace domo.Data;

public partial class HeaterSchedule : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<HeaterScheduleEvent> _events = new();

    public void SetUpPropertyChangedHandler()
    {
        Events.CollectionChanged += (_, e) => OnPropertyChanged(nameof(Events));
    }

    public void AddEvent(HeaterScheduleEvent scheduleEvent)
    {
        if (scheduleEvent.StartTime >= scheduleEvent.EndTime)
        {
            throw new ArgumentException("Start time must be earlier than end time.");
        }

        if (Events.Any(e => scheduleEvent.StartTime <= e.EndTime && scheduleEvent.EndTime >= e.StartTime))
        {
            throw new ArgumentException("New event overlaps with an existing event.");
        }

        Events.Add(scheduleEvent);
    }
}

public class HeaterScheduleEvent
{
    public HeaterLevel Level { get; init; }

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }
}

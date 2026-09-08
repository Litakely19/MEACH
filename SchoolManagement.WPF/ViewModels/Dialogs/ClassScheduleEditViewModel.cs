using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.DTOs.Schedules;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.WPF.Mvvm;
using SchoolManagement.WPF.Services;

namespace SchoolManagement.WPF.ViewModels.Dialogs;

public record TimeOption(TimeSpan Value, string Label);

public class ClassScheduleEditViewModel : DialogViewModelBase
{
    private readonly IScopedExecutor _scopedExecutor;

    private int _groupId;
    private int? _scheduleId;
    private DayOfWeek _dayOfWeek = DayOfWeek.Monday;
    private TimeOption? _startTime;
    private TimeOption? _endTime;

    public ClassScheduleEditViewModel(
        ILogger<ClassScheduleEditViewModel> logger,
        IScopedExecutor scopedExecutor) : base(logger)
    {
        _scopedExecutor = scopedExecutor;
        DialogWidth = 420;
        TimeSlots = BuildTimeSlots();
        _startTime = TimeSlots.FirstOrDefault(slot => slot.Value == TimeSpan.FromHours(9));
        _endTime = TimeSlots.FirstOrDefault(slot => slot.Value == TimeSpan.FromHours(10));
    }

    public IReadOnlyList<DayOfWeek> Days { get; } = Enum.GetValues<DayOfWeek>();

    public IReadOnlyList<TimeOption> TimeSlots { get; }

    public DayOfWeek DayOfWeek
    {
        get => _dayOfWeek;
        set => SetProperty(ref _dayOfWeek, value);
    }

    public TimeOption? StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public TimeOption? EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    public void InitializeForCreate(int studentGroupId, string groupName)
    {
        _groupId = studentGroupId;
        _scheduleId = null;
        Title = $"Add a session — {groupName}";
        ConfirmButtonText = "Add the session";
        DayOfWeek = DayOfWeek.Monday;
        StartTime = TimeSlots.FirstOrDefault(slot => slot.Value == TimeSpan.FromHours(9));
        EndTime = TimeSlots.FirstOrDefault(slot => slot.Value == TimeSpan.FromHours(10));
    }

    public void InitializeForEdit(ClassScheduleDto schedule)
    {
        _groupId = schedule.StudentGroupId;
        _scheduleId = schedule.Id;
        Title = "Edit class session";
        ConfirmButtonText = "Save";
        DayOfWeek = schedule.DayOfWeek;
        StartTime = TimeSlots.FirstOrDefault(slot => slot.Value == schedule.StartTime)
                    ?? new TimeOption(schedule.StartTime, Format(schedule.StartTime));
        EndTime = TimeSlots.FirstOrDefault(slot => slot.Value == schedule.EndTime)
                  ?? new TimeOption(schedule.EndTime, Format(schedule.EndTime));
    }

    protected override async Task<bool> SaveAsync()
    {
        if (StartTime is null || EndTime is null)
        {
            ErrorMessage = "Select the start and end times.";
            return false;
        }

        if (EndTime.Value <= StartTime.Value)
        {
            ErrorMessage = "The end time must be after the start time.";
            return false;
        }

        await _scopedExecutor.RunAsync(async provider =>
        {
            var service = provider.GetRequiredService<IClassScheduleService>();

            if (_scheduleId is null)
            {
                await service.CreateAsync(new CreateClassScheduleRequest(
                    _groupId,
                    DayOfWeek,
                    StartTime.Value,
                    EndTime.Value));
                return;
            }

            await service.UpdateAsync(new UpdateClassScheduleRequest(
                _scheduleId.Value,
                DayOfWeek,
                StartTime.Value,
                EndTime.Value));
        });

        return true;
    }

    private static IReadOnlyList<TimeOption> BuildTimeSlots()
    {
        var slots = new List<TimeOption>();

        for (var minutes = 7 * 60; minutes <= 18 * 60; minutes += 15)
        {
            var value = TimeSpan.FromMinutes(minutes);
            slots.Add(new TimeOption(value, Format(value)));
        }

        return slots;
    }

    private static string Format(TimeSpan time) => $"{(int)time.TotalHours:00}:{time.Minutes:00}";
}

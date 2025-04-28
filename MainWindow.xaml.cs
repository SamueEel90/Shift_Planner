using MongoDB.Driver;
using Shift_Planner.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Shift_Planner;

/// <summary>

/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        FetchShifts();
    }

    public ObservableCollection<Shift> Shifts { get; set; } = new ObservableCollection<Shift>();
    public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();

    private void FetchShifts()
    {
        var userCollection = App.Database.GetCollection<User>("users");

        var users = userCollection.Find(_ => true).ToList();

        Shifts.Clear();

        foreach (var user in users)
        {
            Users.Add(user);
        }
    }

    private List<Shift> GetMonthlyShiftsForUser(string userId, int year, int month, List<Shift> allShifts)
    {
        var shiftsForUser = allShifts
            .Where(shift => shift.UserId == userId && shift.ShiftDate.Year == year && shift.ShiftDate.Month == month)
            .ToList();

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var fullMonthShifts = new List<Shift>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);

            var existingShift = shiftsForUser.FirstOrDefault(shift => shift.ShiftDate.Date == date.Date);

            if (existingShift != null)
            {
                fullMonthShifts.Add(existingShift);
            }
            else
            {
                fullMonthShifts.Add(new Shift
                {
                    UserId = userId,
                    ShiftDate = date,
                    Username = "",
                    Position = "",
                    ArrivalConfirmed = false,
                    DepartureConfirmed = false
                });
            }
        }

        return fullMonthShifts;
    }

    private void FetchShiftsForUser(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is User selectedUser)
        {
            var shiftCollection = App.Database.GetCollection<Shift>("shifts");

            var shifts = shiftCollection
                .Find(shift => shift.UserId == selectedUser.Id)
                .ToList();

            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            var fullMonthShifts = GetMonthlyShiftsForUser(selectedUser.Id, year, month, shifts);

            Shifts.Clear();
            foreach (var shift in fullMonthShifts)
            {
                Shifts.Add(shift);
            }
        }
    }
}
using MongoDB.Driver;
using Shift_Planner.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Shift_Planner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        WindowState = WindowState.Maximized;
        InitializeComponent();
        DataContext = this;
        FetchShifts();
        InitializeAvailableTimes();
    }

    public ObservableCollection<Shift> Shifts { get; set; } = new ObservableCollection<Shift>();
    public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();
    public string SelectedMonth { get; set; } = DateTime.Today.Month.ToString();
    public ObservableCollection<string> AvailableTimes { get; set; } = new ObservableCollection<string>();

    public Dictionary<string, int> Months { get; set; } = new Dictionary<string, int>
    {
        {"Január", 1},
        {"Február", 2},
        {"Marec", 3},
        {"Apríl", 4},
        {"Máj", 5},
        {"Jún", 6},
        {"Júl", 7},
        {"August", 8},
        {"September", 9},
        {"Október", 10},
        {"November", 11},
        {"December", 12}
    };

    private void InitializeAvailableTimes()
    {
        for (int hour = 6; hour <= 22; hour++)
        {
            AvailableTimes.Add($"{hour:00}:00");
        }
        AvailableTimes.Add("Dovolenka");
    }

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
                    ShiftStart = null,
                    ShiftEnd = null,
                    Username = "",
                    Position = "PPO",
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
            int month = Months.TryGetValue(SelectedMonth, out int m) ? m : DateTime.Now.Month;

            var fullMonthShifts = GetMonthlyShiftsForUser(selectedUser.Id, year, month, shifts);

            Shifts.Clear();
            foreach (var shift in fullMonthShifts)
            {
                Shifts.Add(shift);
            }
        }
    }

    private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is string selectedMonth)
        {
            int monthNumber = Months[selectedMonth];
        }
    }

    private void SubmitShiftsClick(object sender, RoutedEventArgs e)
    {
        var shiftCollection = App.Database.GetCollection<Shift>("shifts");

        if (NameComboBox.SelectedItem is User selectedUser)
        {
            foreach (var shift in Shifts)
            {
                if (shift.UserId != selectedUser.Id)
                    continue;

                if (shift.ShiftStart != null && shift.ShiftEnd != null)
                {
                    var existingShift = shiftCollection
                        .Find(s => s.UserId == shift.UserId && s.ShiftDate == shift.ShiftDate)
                        .FirstOrDefault();

                    if (existingShift == null)
                    {
                        shiftCollection.InsertOne(new Shift
                        {
                            UserId = shift.UserId,
                            Username = selectedUser.Name,
                            ShiftDate = new DateTime(
                                shift.ShiftDate.Year,
                                shift.ShiftDate.Month,
                                shift.ShiftDate.Day,
                                12, 0, 0, DateTimeKind.Local
                            ).ToUniversalTime(),
                            
                            ShiftStart = shift.ShiftStart.Value.AddHours(2),
                            ShiftEnd = shift.ShiftEnd.Value.AddHours(2),
                            Position = "PPO",
                            ArrivalConfirmed = false,
                            DepartureConfirmed = false
                        });
                    }
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a user to submit shifts.");
        }
    }
}

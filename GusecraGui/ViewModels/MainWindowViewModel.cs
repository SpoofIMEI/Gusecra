namespace GusecraGui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public string _message = "";
    
    [ObservableProperty]
    public string _password = "";

    [ObservableProperty]
    public int _repeat = 1;

    [ObservableProperty]
    public int _port = 7322;

    [ObservableProperty]
    public string _ip = "127.0.0.1";

    [ObservableProperty]
    public string _callsign = "Anonymous";

    [ObservableProperty]
    public string _file = "";
}

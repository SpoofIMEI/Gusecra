using Avalonia.Controls;
using GusecraGui.ViewModels;
using Avalonia.Interactivity;
using System;
using Gusecra;
using Gusecra.Models;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Threading;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;


namespace GusecraGui.Views;

public partial class MainWindow : Window
{
    GusecraConnection? con = null;
    bool sending = false;
    bool listening = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void listen() {
        if (listening) 
            return;
        listening = true;
        await Task.Run(()=>{
            while (con == null) 
                Task.Delay(200);
            
            while (true) {
                Request req;
                try {
                    req = con.Read();
                }catch{
                    break;
                }
                string attached = ""; 
                if (req.File != null) {
                    var file = req.File.GetValueOrDefault();
                    var filename = Path.GetFileName(file.FileName);
                    File.WriteAllBytes(filename, file.Content);
                    attached =  $"\n\nAttached file: {file.FileName}\nDownloaded to: {Path.GetFullPath(file.FileName)}";
                }
                Dispatcher.UIThread.Invoke(()=>{
                    msg($"{req.Callsign}:\n{Encoding.UTF8.GetString(req.Message)}{attached}");
                });
            }
            listening = false;
        });
    }

    public void RemoveAttachHandler(object sender, RoutedEventArgs args) {
        var context = (MainWindowViewModel)DataContext;
        context.File = "";
    }

    public async void AttachHandler(object sender, RoutedEventArgs args) {
        var context = (MainWindowViewModel)DataContext;
        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open file to send as attachment",
            AllowMultiple = false
        });
        if (file.Count == 0) 
            return;
        
        context.File = file[0].TryGetLocalPath();
    }

    public void PasswordHandler(object sender, RoutedEventArgs args) {
        var context = (MainWindowViewModel)DataContext;
        try {
            con.SetPassword(context.Password);
        }catch {
            msg($"Error: Not connected!", "red");
            return;
        }
        
        msg("Password set!", "lime");
    }

    public async void SendHandler(object sender, RoutedEventArgs args) {
        if (sending) 
            msg("ignoring transmit, because it happened 200 ms after the previous transmit");
        var context = (MainWindowViewModel)DataContext;    
        AttachedFile? file = null;
        if (context.File != "") 
            file = new(Path.GetFileName(context.File), File.ReadAllBytes(context.File));
        
        Request req = new(status: true, 
                    message: Encoding.UTF8.GetBytes(context.Message), 
                    callsign: context.Callsign, 
                    file: file);

        try {
            con.Write(req, context.Repeat);
        }catch {
            msg($"Error: Not connected!", "red");
            return;
        }
        string attached = context.File == "" ? "" : $"\n\nAttached file: {context.File}";
        msg($"{context.Callsign}:\n{context.Message}{attached}", "green");
        context.Message = "";

        sending = true;
        await Task.Delay(200);
        sending = false;
    }

    public async void ConnectHandler(object sender, RoutedEventArgs args) {
        var context = (MainWindowViewModel)DataContext;
        if (con != null)
            con.Close();
        try {
            con = new(context.Ip, context.Port);
        }catch(Exception e) {
            msg($"Error: {e.Message}", "red");
            return;
        }
        await Task.Delay(250);
        con.SetPassword(context.Password);
        listen();
        msg("Connected succesfully!", "lime");
    }

    private void msg(string msg, string color="white") {
        var line = new SelectableTextBlock();
        line.Foreground = Brush.Parse(color);
        line.Text = $"{DateTime.Now}: {msg}";
        line.Margin = new(0,2);
        output.Children.Insert(0, line);
    }
}
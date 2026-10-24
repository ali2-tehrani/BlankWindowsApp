namespace BlankWindowsApp;

public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "Blank Windows App";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(800, 450);
        MinimumSize = new Size(400, 300);
    }
}

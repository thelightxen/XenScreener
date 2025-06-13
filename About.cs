using System.Reflection;

namespace XenScreener;

public partial class About : Form
{
    public About()
    {
        InitializeComponent();
    }

    private void About_Load(object sender, EventArgs e)
    {
        TitleProgram.Text += Assembly.GetExecutingAssembly().GetName().Version;
    }
}
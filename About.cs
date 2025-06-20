using System.Reflection;

namespace XenScreener;

public partial class About : Form
{
    public About()
    {
        InitializeComponent();
        FormClosed += (s, e) => Dispose();
    }

    private void About_Load(object sender, EventArgs e)
    {
        TitleProgram.Text += Assembly.GetExecutingAssembly().GetName().Version;
    }
    
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.Dispose();
        base.OnFormClosed(e);
    }


}
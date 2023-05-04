using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Template = HarmonyExtension.Template;

namespace HarmonyExtension.Options
{
    /// <summary>
    /// Interaction logic for TemplateOptions.xaml
    /// </summary>
    public partial class TemplateOptions : UserControl
    {
        public TemplateOptions()
        {
            InitializeComponent();
        }
        internal TemplateOptionsPage optionsPage;

        public void Initialize()
        {
            tbAttribute.Text = HarmonyExtension.Template.Instance.AttributeTemplate;
            tbManual.Text = HarmonyExtension.Template.Instance.ManualTemplate;
            System.Windows.Forms.MessageBox.Show("Opened");
        }

        private void ManualTemplate_TextChanged(object sender, TextChangedEventArgs e)
        {
            HarmonyExtension.Template.Instance.ManualTemplate = tbManual.Text;
            HarmonyExtension.Template.Instance.Save();
        }

        private void AttributeTemplate_TextChanged(object sender, TextChangedEventArgs e)
        {
            HarmonyExtension.Template.Instance.AttributeTemplate = tbAttribute.Text;
            HarmonyExtension.Template.Instance.Save();
        }

        //Todo: Find a way to allow enter.  This catches keys but can't eat Enter
        //PreviewKeyDown="Window_PreviewKeyDown"
        //private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        e.Handled = true; // prevent the form from closing
        //        //submitButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // invoke the click event of the button
        //    }
        //}
    }
}

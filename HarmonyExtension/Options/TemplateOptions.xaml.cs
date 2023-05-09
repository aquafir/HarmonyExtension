using System.Windows;
using System.Windows.Controls;

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
            tbAttribute.Text = HarmonyExtension.Template.Instance.AnnotatedTemplate;
            tbManual.Text = HarmonyExtension.Template.Instance.ManualTemplate;
        }

        private void ManualTemplate_TextChanged(object sender, TextChangedEventArgs e)
        {
            HarmonyExtension.Template.Instance.ManualTemplate = tbManual.Text;
            PreviewTemplate(tbManual.Text);
            HarmonyExtension.Template.Instance.Save();
        }

        private void AttributeTemplate_TextChanged(object sender, TextChangedEventArgs e)
        {
            HarmonyExtension.Template.Instance.AnnotatedTemplate = tbAttribute.Text;
            PreviewTemplate(tbAttribute.Text);
            HarmonyExtension.Template.Instance.Save();
        }

        /// <summary>
        /// Use default variables to preview the last modified template
        /// </summary>
        private void PreviewTemplate(string text)
        {
            lTemplatePreview.Content = text.PreviewTemplate();
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            HarmonyExtension.Template.Instance.AnnotatedTemplate = TemplateHelpers.AnnotatedTemplateDefault;
            HarmonyExtension.Template.Instance.ManualTemplate = TemplateHelpers.ManualTemplateDefault;
            tbAttribute.Text = HarmonyExtension.Template.Instance.AnnotatedTemplate;
            tbManual.Text = HarmonyExtension.Template.Instance.ManualTemplate;
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

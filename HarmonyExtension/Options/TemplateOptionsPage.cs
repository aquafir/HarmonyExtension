using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using UIElement = System.Windows.UIElement;
using System.Windows.Input;

namespace HarmonyExtension.Options
{
    [ComVisible(true)]
    [Guid("6277c470-ccef-4048-9bf3-535613e34fde")]
    public class TemplateOptionsPage : UIElementDialogPage
    {
        protected override UIElement Child
        {
            get
            {
                TemplateOptions page = new TemplateOptions
                {
                    optionsPage = this
                };
                page.Initialize();

                return page;
            }
        }
    }
}

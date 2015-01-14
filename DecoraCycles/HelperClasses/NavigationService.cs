using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoraCsycles.HelperClasses
{
    public class NavigationService : INavigationService
    {
        public bool CanGoBack
        {
            get
            {
                if (App.RootFrame == null)
                    return false;
                return App.RootFrame.CanGoBack;
            }
        }

        public Uri CurrentPageType
        {
            get
            {
                return App.RootFrame.CurrentSource;
            }
        }

        public virtual void GoBack()
        {
            if (App.RootFrame.CanGoBack)
            {
                App.RootFrame.GoBack();
            }
        }

        public virtual void NavigateTo(Uri sourcePageType)
        {
            if (App.RootFrame.CurrentSource == null || Path.GetFileName(sourcePageType.OriginalString) != Path.GetFileName(App.RootFrame.CurrentSource.OriginalString))
                App.RootFrame.Navigate(sourcePageType);
        }

        public virtual void NavigateTo(Uri sourcePageType, object parameter)
        {
            App.RootFrame.Navigate(sourcePageType, parameter);
        }

        public virtual void GoForward()
        {
            if (App.RootFrame.CanGoForward)
            {
                App.RootFrame.GoForward();
            }
        }

        public virtual void GoHome()
        {
            while (App.RootFrame.CanGoBack)
            {
                App.RootFrame.GoBack();
            }
        }


    }
}

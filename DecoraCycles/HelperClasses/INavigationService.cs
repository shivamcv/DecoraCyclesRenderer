using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecoraCsycles.HelperClasses
{
    public interface INavigationService
    {
        void GoBack();
        bool CanGoBack { get; }
        void NavigateTo(Uri pageType);
        void NavigateTo(Uri sourcePageType, object parameter);


    }
}

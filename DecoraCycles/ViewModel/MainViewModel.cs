using ccl;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace DecoraCsycles.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        private readonly HelperClasses.INavigationService _navigationService;
      
        public MainViewModel()
        {
            //
            //uint id =0;
            //new DecoraCsycles.Model.CyclesShader().GetMaterial("Materials/Diffuse.bcm", ref id);
            //


            _navigationService = ServiceLocator.Current.GetInstance<HelperClasses.INavigationService>();
            CameraView = Transform.Translate(new float4(0, 0, -800));

            MaterialList =  Directory.GetFiles("Materials").ToList();
        }

        private RelayCommand<string> navigate;
        public RelayCommand<string> Navigate
        {
            get
            {
                return navigate ?? (navigate = new RelayCommand<string>((path) =>
                {
                    _navigationService.NavigateTo(new Uri("pack://application:,,,/DecoraCsycles;component/" + path, UriKind.Absolute));
                }));
            }
        }

        public enum transformType
        {
            rotate,scale,translate
        }

        private string finalView;

        public string FinalView
        {
            get { return finalView; }
            set { finalView = value;
            RaisePropertyChanged("FinalView");
            }
        }

        public string Translate { get; set; }

        public string Rotate { get; set; }

        private RelayCommand loadCamera;

        public RelayCommand LoadCamera
        {
            get
            {
                return loadCamera ?? (loadCamera = new RelayCommand(() =>
                    {
                        if (string.IsNullOrEmpty(Translate) && string.IsNullOrEmpty(Rotate))
                            CameraView = null;
                        else
                        {
                            var t = getTransform(Translate, transformType.translate );
                            var r = getTransform(Rotate, transformType.rotate);

                            if (r == null && t != null)
                                CameraView = t;
                            else if (r != null && t == null)
                                CameraView = r;
                            else
                            CameraView = r * t;
                        }
                    }));
            }
        }

        private Transform getTransform(string str, transformType transformType)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            else
            {
                var val = str.Split(',').Select(p => float.Parse(p)).ToArray();

                if (transformType == MainViewModel.transformType.rotate)
                    return Transform.Rotate(val[0], new float4(val[1], val[2], val[3]));
                else
                    return Transform.Translate(val[0], val[1], val[2]);
            }
        }

        public Transform CameraView { get; set; }

        public List<string>  MaterialList { get; set; }

        private string selectedMaterial;

        public string SelectedMaterial
        {
            get { return selectedMaterial; }
            set { selectedMaterial = value;
            RaisePropertyChanged("SelectedMaterial");
            MaterialText = File.ReadAllText(selectedMaterial);
            }
        }

        private string materialText;

        public string MaterialText
        {
            get { return materialText; }
            set { materialText = value;
            RaisePropertyChanged("MaterialText");
            }
        }

        private RelayCommand applyMaterial;

        public RelayCommand ApplyMaterial
        {
            get
            {
                return applyMaterial ?? (applyMaterial = new RelayCommand(() =>
                    {
                          Task<int>.Factory.StartNew(SimpleIoc.Default.GetInstance<ConnectionViewModel>().loadDefaultData, SelectedMaterial);
                    }));
            }
        }

        private RelayCommand render;

        public RelayCommand Render
        {
            get
            {
                return render ?? (render = new RelayCommand(() =>
                    {
                        Task<int>.Factory.StartNew(SimpleIoc.Default.GetInstance<ConnectionViewModel>().loadDefaultData, null);
                    }));
            }
        }
        
    }
}
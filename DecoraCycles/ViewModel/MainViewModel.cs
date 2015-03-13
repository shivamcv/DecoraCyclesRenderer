using ccl;
using DecoraCsycles.Properties;
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
            Translate = Settings.Default.Translate;
            Rotate = Settings.Default.Rotate;
            FixedView = Settings.Default.FixedView;
            RaisePropertyChanged("Translate");
            RaisePropertyChanged("Rotate");

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
            set {
                var t = value.Replace("[", "");
                 t = t.Replace("]", "");
                foreach (var item in t.Split(','))
                {
                    finalView += item + "f,";
                }
                finalView += "\n";
            RaisePropertyChanged("FinalView");
            }
        }

        private string fixedView;// = "0.8453702f, -0.01766679f, 0.5338884f, 6.543896f, -0.02637207f, -0.9996144f, 0.008680073f, -65.50546f, 0.5335293f, -0.02141762f, -0.8455103f, 46.41003f, 0f, 0f, 0f, 1f";

        public string FixedView
        {
            get { return fixedView; }
            set { fixedView = value;
            RaisePropertyChanged("FixedView");
            Settings.Default.FixedView = value;
            Settings.Default.Save();
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

                        Settings.Default.Rotate = Rotate;
                        Settings.Default.Translate = Translate;
                        Settings.Default.Save();

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
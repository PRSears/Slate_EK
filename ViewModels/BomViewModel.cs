using Extender.WPF;
using Slate_EK.Models;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public class BomViewModel : ViewModel
    {
        #region shortcut commands
        public ICommand Shortcut_CtrlK  { get; private set; }
        public ICommand Shortcut_CtrlS  { get; private set; }

        public event ShortcutEventHandler ShortcutPressed_CtrlK;
        public event ShortcutEventHandler ShortcutPressed_CtrlS;

        #endregion

        public string WindowTitle
        {
            get
            {
                return string.Format
                (
                    "Assembly #{1} [{2}] - {0}",
                    Properties.Settings.Default.ShortTitle,
                    Bom.AssemblyNumber,
                    Bom.Fasteners.Count.ToString()
                );
            }
        }

        public bool OverrideLength
        {
            get
            {
                return _OverrideLength;
            }
            set
            {
                _OverrideLength = value;
                OnPropertyChanged("OverrideLength");
                OnPropertyChanged("LengthOverrideVisibility");
            }
        }

        public Fastener WorkingFastener
        {
            get
            {
                return _WorkingFastener;
            }
            set
            {
                _WorkingFastener = value;
                OnPropertyChanged("WorkingFastener");
            }
        }

        public Bom Bom
        {
            get
            {
                return _Bom;
            }
            set
            {
                _Bom = value;
                OnPropertyChanged("Bom");
            }
        }

        #region boxed properties

        private Bom         _Bom;
        private Fastener    _WorkingFastener;
        private bool        _OverrideLength;

        #endregion
        
        public BomViewModel() : this(string.Empty)
        { }

        public BomViewModel(string assemblyNumber)
        {
            this.Bom = new Models.Bom(assemblyNumber);

            Bom.PropertyChanged += (s, e) =>
            {   //Link AssemblyNumber propertychanged to WindowTitle
                if (e.PropertyName.Equals("AssemblyNumber"))
                    OnPropertyChanged("WindowTitle");
            };

            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Init shortcuts
            Shortcut_CtrlK = new RelayCommand
            (
                () => NullsafeHandleShortcut(ShortcutPressed_CtrlK)
            );

            Shortcut_CtrlS = new RelayCommand
            (
                () => NullsafeHandleShortcut(ShortcutPressed_CtrlS)
            );

            ShortcutPressed_CtrlS += () =>
            {
                System.Windows.MessageBox.Show("Save shortcut not yet implemented");
            };
        }

        private bool NullsafeHandleShortcut(ShortcutEventHandler handler)
        {
            if (handler != null)
            {
                handler();
                return true;
            }
            else return false;
        }
    }

    public delegate void ShortcutEventHandler();
}

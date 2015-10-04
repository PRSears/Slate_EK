using Slate_EK.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BomView
    {
        private BomViewModel ViewModel
        {
            get
            {
                if (DataContext is BomViewModel)
                    return (BomViewModel)DataContext;
                return null;
            }
            set
            {
                DataContext = value;
            }
        }

        public BomView() : this(string.Empty)
        { }

        public BomView(string assemblyNumber)
        {
            Activated += (sender, e) =>
            {
                PlateThicknessTextField.Focus();
            };

            ViewModel = new BomViewModel(assemblyNumber);
            
            InitializeComponent();

            ViewModel.RegisterCloseAction(Close);
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals("OverrideLength"))
                {
                    LengthTextbox.Opacity = ViewModel.OverrideLength ? 1d : 0.35;
                }
                else if (e.PropertyName.Equals("ObservableFasteners"))
                {
                    FastenerItemsControl.ItemsSource = ViewModel.ObservableFasteners;
                }
            };
            ViewModel.ShortcutPressedCtrlK += () => AssemblyNumberField.Focus();
            ViewModel.OverrideLength = false;

            FastenerItemsControl.ItemsSource   = ViewModel.ObservableFasteners;
            MaterialsDropdown.SelectedIndex    = 0; // HACK to fix a bug where the dropdown had no SelectedValue,
                                                         // despite array being initialized, etc.
        }

        #region // Click / drag / selection handling

        private bool  _IsMouseDown;
        private Point _MouseDownPos;

        private int _SelectDownIndex   = -1;
        private int _SelectUpIndex     = -1;

        private List<FastenerControl> _PassedOver;
        private List<FastenerControl> _PreviouslySelected;

        private bool HasFastenerSelected
        {
            get
            {
                return FastenerItemsControl.Items.Cast<FastenerControl>().Any(item => item.IsSelected);
            }
        }

        private int  SelectedFastenersCount
        {
            get
            {
                return FastenerItemsControl.Items.Cast<FastenerControl>().Count(item => item.IsSelected);
            }
        }

        private int  FirstSelectedIndex
        {
            get
            {
                if (!HasFastenerSelected) return -1;

                return FastenerItemsControl.Items.IndexOf
                (
                    _PreviouslySelected.First()
                );
            }
        }

        private int  LastSelectedIndex
        {
            get
            {
                if (!HasFastenerSelected) return -1;

                return FastenerItemsControl.Items.IndexOf
                (
                    _PreviouslySelected.Last()
                );
            }
        }

        private void FastenersBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FastenersBox.CaptureMouse();

            _IsMouseDown             = true;
            _MouseDownPos            = e.GetPosition(FastenersBox);
            _PreviouslySelected      = new List<FastenerControl>();
            _PassedOver              = new List<FastenerControl>();

            foreach (FastenerControl item in FastenerItemsControl.Items)
            {
                if (item.IsSelected)
                    _PreviouslySelected.Add(item);
            }

            Canvas.SetLeft(SelectionBox, _MouseDownPos.X);
            Canvas.SetTop(SelectionBox, _MouseDownPos.Y);

            SelectionBox.Width  = 0;
            SelectionBox.Height = 0;

            SelectionBox.Visibility = Visibility.Visible;

            Extender.Debugging.Debug.WriteMessage($"_MouseDown [{_MouseDownPos.X}, {_MouseDownPos.Y}]", Debug, "info");

            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement origin = GetRootFastenerElement(e.OriginalSource as FrameworkElement);

                if (origin != null)
                {
                    _SelectDownIndex = FastenerItemsControl.Items.IndexOf(origin.DataContext);
                }
            }

            HandleSelections(true);

            //
            // Handle double click
            if (e.ClickCount == 2 && SelectedFastenersCount == 1)
            {
                _PreviouslySelected.First().RequestQuantityChange.Execute(null);

                _IsMouseDown = false;
                SelectionBox.Visibility = Visibility.Collapsed;
            }
        }

        private void FastenersBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_IsMouseDown)
            {
                _IsMouseDown             = false;
                SelectionBox.Visibility = Visibility.Collapsed;

                Point mouseUpPos = e.GetPosition(FastenersBox);

                Extender.Debugging.Debug.WriteMessage($"_MouseUp [{mouseUpPos.X}, {mouseUpPos.Y}]", Debug, "info");

                if (e.OriginalSource is FrameworkElement)
                {
                    FrameworkElement origin = GetRootFastenerElement(e.OriginalSource as FrameworkElement);

                    if (origin != null)
                        _SelectUpIndex = FastenerItemsControl.Items.IndexOf(origin.DataContext); 
                }

                Extender.Debugging.Debug.WriteMessage($"Selection started at index [{_SelectDownIndex}] and ended at [{_SelectUpIndex}].", Debug);

                if (_SelectDownIndex >= 0 &&
                    HasFastenerSelected &&
                    (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) &&
                    mouseUpPos.Y.Equals(_MouseDownPos.Y))
                {
                    // Single click with shift held
                    if (_SelectDownIndex > LastSelectedIndex)
                    {
                        for (int i = LastSelectedIndex; i < _SelectDownIndex; i++)
                        {
                            (FastenerItemsControl.Items[i] as FastenerControl)?.SelectCommand.Execute(null);
                        }
                    }
                    else if (_SelectDownIndex < FirstSelectedIndex)
                    {
                        for (int i = _SelectDownIndex; i < FirstSelectedIndex; i++)
                        {
                            (FastenerItemsControl.Items[i] as FastenerControl)?.SelectCommand.Execute(null);
                        }
                    }
                }

                _SelectDownIndex = -1;
                _SelectUpIndex   = -1;
            }
        }

        private void FastenersBox_RightMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement &&
               ((FrameworkElement)e.OriginalSource).DataContext != null &&
               ((FrameworkElement)e.OriginalSource).DataContext is FastenerControl &&
               (SelectedFastenersCount <= 1))
            {
                ClearFastenersSelection();
                (((FrameworkElement)e.OriginalSource).DataContext as FastenerControl)?.SelectCommand.Execute(null);
            }
        }

        private void FastenersBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_IsMouseDown)
            {
                Point curPos = e.GetPosition(FastenersBox);

                //
                // resize the selection box
                if (_MouseDownPos.X < curPos.X)
                {
                    Canvas.SetLeft(SelectionBox, _MouseDownPos.X);
                    SelectionBox.Width  = curPos.X - _MouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(SelectionBox, curPos.X);
                    SelectionBox.Width  = _MouseDownPos.X - curPos.X;
                }

                if (_MouseDownPos.Y < curPos.Y)
                {
                    Canvas.SetTop(SelectionBox, _MouseDownPos.Y);
                    SelectionBox.Height = curPos.Y - _MouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(SelectionBox, curPos.Y);
                    SelectionBox.Height = _MouseDownPos.Y - curPos.Y;
                }

                HandleSelections(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">The source element whose tree we're searching.</param>
        /// <returns>
        /// Returns the highest level element in the tree with a FastenerControl as its DataContext.
        /// Return null if 'source' doesn't have a FastenerControl as its DataContext.
        /// </returns>
        private FrameworkElement GetRootFastenerElement(FrameworkElement source)
        {
            if (source.DataContext != null && source.DataContext is FastenerControl)
            {
                FrameworkElement root = source;

                while   (root.Parent != null &&
                        ((FrameworkElement)root.Parent)?.DataContext != null && 
                        ((FrameworkElement)root.Parent)?.DataContext is FastenerControl)
                {
                    root = (FrameworkElement)root.Parent;
                }

                return root;
            }
            return null;
        }

        private void HandleSelections(bool firstClick)
        {
            bool ctrlDown   = false;
            bool shiftDown  = false;

            //
            // Check modifier keys
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Handle CTRL key down
                ctrlDown = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                // Handle Shift key down
                shiftDown = true;
            }
            else
            {
                // Handle no modifiers
                ClearFastenersSelection();
            }

            //
            // Get selection box bounds
            Rect selectionBounds;
            if (firstClick)
            {
                selectionBounds = new Rect(_MouseDownPos, new Size(2, 2));
                // The rectangle used for selection hasn't actually been drawn yet, 
                // so we need to fake it.
            }
            else
            {
                selectionBounds = new Rect
                (
                    SelectionBox.TranslatePoint(new Point(0, 0), FastenersBox),
                    new Size(SelectionBox.ActualWidth, SelectionBox.ActualHeight)
                );
            }

            //
            // Make a list of the elements in InventoryItemsControl so we can hit test them
            FrameworkElement[] fastenerElements = new FrameworkElement[FastenerItemsControl.Items.Count];
            for (int i = 0; i < fastenerElements.Length; i++)
            {
                DependencyObject container = FastenerItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                if (container is FrameworkElement)
                    fastenerElements[i] = container as FrameworkElement;
            }

            //
            // Select and/or deselect 
            foreach (FrameworkElement fastenerElement in fastenerElements)
            {
                FastenerControl fastener = fastenerElement.DataContext as FastenerControl;

                Rect hitBox = new Rect
                (
                    fastenerElement.TranslatePoint(new Point(0, 0), FastenersBox),
                    new Size(fastenerElement.ActualWidth, fastenerElement.ActualHeight)
                );

                bool inSelection = selectionBounds.IntersectsWith(hitBox);

                if (ctrlDown)
                {
                    if (inSelection && !_PreviouslySelected.Contains(fastener))
                    {
                        fastener?.SelectCommand.Execute(null);
                    }
                    else if (inSelection && _PreviouslySelected.Contains(fastener))
                    {
                        fastener?.DeselectCommand.Execute(null);
                    }
                    else if (!inSelection && _PreviouslySelected.Contains(fastener))
                    {
                        fastener?.SelectCommand.Execute(null);
                    }
                    else
                    {
                        fastener?.DeselectCommand.Execute(null);
                    }
                }
                else if (shiftDown)
                {
                    if (inSelection)
                    {
                        fastener?.SelectCommand.Execute(null);
                        _PassedOver.Add(fastener);
                    }
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse // for clarity
                    else if (!inSelection && _PassedOver.Contains(fastener))
                    {
                        fastener?.DeselectCommand.Execute(null);
                    }
                }
                else
                {
                    if (inSelection &&
                        !(!HasFastenerSelected && _PreviouslySelected.Count <= 1 && _PreviouslySelected.Contains(fastener)))
                    {
                        fastener?.SelectCommand.Execute(null);
                    }
                    else fastener?.DeselectCommand.Execute(null);
                }
            }
        }

        private void ClearFastenersSelection()
        {
            foreach (var item in FastenerItemsControl.Items)
            {
                (item as FastenerControl)?.DeselectCommand.Execute(null);
            }
        }

        #endregion


        #region // Settings.Settings aliases
        private bool Debug => Properties.Settings.Default.Debug;

        #endregion
    }
}

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
        
        protected bool  IsMouseDown;
        protected Point MouseDownPos;

        protected int SelectDownIndex   = -1;
        protected int SelectUpIndex     = -1;

        protected List<FastenerControl>     PassedOver;
        protected List<FastenerControl>     PreviouslySelected;

        protected bool HasFastenerSelected
        {
            get
            {
                return FastenerItemsControl.Items.Cast<FastenerControl>().Any(item => item.IsSelected);
            }
        }
        protected int  SelectedFastenersCount
        {
            get
            {
                return FastenerItemsControl.Items.Cast<FastenerControl>().Count(item => item.IsSelected);
            }
        }
        protected int  FirstSelectedIndex
        {
            get
            {
                if (!HasFastenerSelected) return -1;

                return FastenerItemsControl.Items.IndexOf
                (
                    PreviouslySelected.First()
                );
            }
        }
        protected int  LastSelectedIndex
        {
            get
            {
                if (!HasFastenerSelected) return -1;

                return FastenerItemsControl.Items.IndexOf
                (
                    PreviouslySelected.Last()
                );
            }
        }

        private void FastenersBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FastenersBox.CaptureMouse();

            IsMouseDown             = true;
            MouseDownPos            = e.GetPosition(FastenersBox);
            PreviouslySelected      = new List<FastenerControl>();
            PassedOver              = new List<FastenerControl>();

            foreach (FastenerControl item in FastenerItemsControl.Items)
            {
                if (item.IsSelected)
                    PreviouslySelected.Add(item);
            }

            Canvas.SetLeft(SelectionBox, MouseDownPos.X);
            Canvas.SetTop(SelectionBox, MouseDownPos.Y);

            SelectionBox.Width  = 0;
            SelectionBox.Height = 0;

            SelectionBox.Visibility = Visibility.Visible;

            Extender.Debugging.Debug.WriteMessage($"_MouseDown [{MouseDownPos.X}, {MouseDownPos.Y}]", Debug, "info");

            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement origin = GetRootFastenerElement(e.OriginalSource as FrameworkElement);

                if (origin != null)
                {
                    SelectDownIndex = FastenerItemsControl.Items.IndexOf(origin.DataContext);
                }
            }

            HandleSelections(true);

            //
            // Handle double click
            if (e.ClickCount == 2 && SelectedFastenersCount == 1)
            {
                PreviouslySelected.First().RequestQuantityChange.Execute(null);

                IsMouseDown = false;
                SelectionBox.Visibility = Visibility.Collapsed;
            }
        }

        private void FastenersBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseDown)
            {
                IsMouseDown             = false;
                SelectionBox.Visibility = Visibility.Collapsed;

                Point mouseUpPos = e.GetPosition(FastenersBox);

                Extender.Debugging.Debug.WriteMessage($"_MouseUp [{mouseUpPos.X}, {mouseUpPos.Y}]", Debug, "info");

                if (e.OriginalSource is FrameworkElement)
                {
                    FrameworkElement origin = GetRootFastenerElement(e.OriginalSource as FrameworkElement);

                    if (origin != null)
                        SelectUpIndex = FastenerItemsControl.Items.IndexOf(origin.DataContext); 
                }

                Extender.Debugging.Debug.WriteMessage($"Selection started at index [{SelectDownIndex}] and ended at [{SelectUpIndex}].", Debug);

                if (SelectDownIndex >= 0 &&
                    HasFastenerSelected &&
                    (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) &&
                    mouseUpPos.Y.Equals(MouseDownPos.Y))
                {
                    // Single click with shift held
                    if (SelectDownIndex > LastSelectedIndex)
                    {
                        for (int i = LastSelectedIndex; i < SelectDownIndex; i++)
                        {
                            (FastenerItemsControl.Items[i] as FastenerControl)?.SelectCommand.Execute(null);
                        }
                    }
                    else if (SelectDownIndex < FirstSelectedIndex)
                    {
                        for (int i = SelectDownIndex; i < FirstSelectedIndex; i++)
                        {
                            (FastenerItemsControl.Items[i] as FastenerControl)?.SelectCommand.Execute(null);
                        }
                    }
                }

                SelectDownIndex = -1;
                SelectUpIndex   = -1;
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
            if (IsMouseDown)
            {
                Point curPos = e.GetPosition(FastenersBox);

                //
                // resize the selection box
                if (MouseDownPos.X < curPos.X)
                {
                    Canvas.SetLeft(SelectionBox, MouseDownPos.X);
                    SelectionBox.Width = curPos.X - MouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(SelectionBox, curPos.X);
                    SelectionBox.Width = MouseDownPos.X - curPos.X;
                }

                if (MouseDownPos.Y < curPos.Y)
                {
                    Canvas.SetTop(SelectionBox, MouseDownPos.Y);
                    SelectionBox.Height = curPos.Y - MouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(SelectionBox, curPos.Y);
                    SelectionBox.Height = MouseDownPos.Y - curPos.Y;
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
                selectionBounds = new Rect(MouseDownPos, new Size(2, 2));
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
                    if (inSelection && !PreviouslySelected.Contains(fastener))
                    {
                        fastener?.SelectCommand.Execute(null);
                    }
                    else if (inSelection && PreviouslySelected.Contains(fastener))
                    {
                        fastener?.DeselectCommand.Execute(null);
                    }
                    else if (!inSelection && PreviouslySelected.Contains(fastener))
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
                        PassedOver.Add(fastener);
                    }
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse // for clarity
                    else if (!inSelection && PassedOver.Contains(fastener))
                    {
                        fastener?.DeselectCommand.Execute(null);
                    }
                }
                else
                {
                    if (inSelection &&
                        !(!HasFastenerSelected && PreviouslySelected.Count <= 1 && PreviouslySelected.Contains(fastener)))
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
